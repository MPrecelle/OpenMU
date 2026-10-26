// <copyright file="AdminUserControllerTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Api;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MUnique.OpenMU.Persistence.AdminAuth;
using MUnique.OpenMU.Web.AdminPanel.API;
using MUnique.OpenMU.Web.AdminPanel.Auth;
using MUnique.OpenMU.Web.Tests.AdminAuth;

/// <summary>
/// Tests for the <see cref="AdminUserController"/>, which logs in the admin panel users for our web.
/// </summary>
[TestFixture]
public class AdminUserControllerTests
{
    private const string TestPassword = "a-very-long-test-password";

    private ServiceProvider _serviceProvider = null!;

    /// <summary>
    /// Sets a fresh service provider up for each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddDataProtection();
        services.AddSingleton<IAdminUserRepository>(new InMemoryAdminUserRepository());
        services.AddSingleton(Options.Create(new AdminPanelAuthOptions()));
        services.AddSingleton<AdminUserSecretProtector>();
        services.AddSingleton<IPasswordHasher<AdminUser>, BCryptPasswordHasher>();
        services.AddSingleton<BootstrapAdminUserProvider>();
        services.AddSingleton<SignInTicketService>();
        services.AddScoped<IUserStore<AdminUser>, AdminUserStore>();
        services.AddScoped<AdminLoginService>();
        services.AddScoped<AuthenticatorSetupService>();
        services.AddIdentityCore<AdminUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 3;
            })
            .AddDefaultTokenProviders();
        this._serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Disposes the service provider.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        this._serviceProvider.Dispose();
    }

    /// <summary>
    /// A user without a second factor logs in with its password, and gets its effective roles.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task PasswordAsync()
    {
        await this.CreateUserAsync("martin", withAuthenticator: false).ConfigureAwait(false);

        var result = await this.VerifyAsync("martin", TestPassword).ConfigureAwait(false);

        Assert.That(result.Value!.Status, Is.EqualTo(AdminLoginStatus.Succeeded));
        Assert.That(result.Value.LoginName, Is.EqualTo("martin"));
        Assert.That(result.Value.Roles, Does.Contain(AdminRoles.Administrator).And.Contain(AdminRoles.Operator));
    }

    /// <summary>
    /// A wrong password or an unknown user is answered with <c>401</c>.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task WrongPasswordAsync()
    {
        await this.CreateUserAsync("martin", withAuthenticator: false).ConfigureAwait(false);

        Assert.That(Status(await this.VerifyAsync("martin", "wrong-password").ConfigureAwait(false)), Is.EqualTo(AdminLoginStatus.Failed));
        Assert.That(Status(await this.VerifyAsync("nobody", TestPassword).ConfigureAwait(false)), Is.EqualTo(AdminLoginStatus.Failed));
    }

    /// <summary>
    /// A user with a second factor needs the code of the authenticator.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task SecondFactorAsync()
    {
        var key = await this.CreateUserAsync("martin", withAuthenticator: true).ConfigureAwait(false);

        Assert.That(Status(await this.VerifyAsync("martin", TestPassword).ConfigureAwait(false)), Is.EqualTo(AdminLoginStatus.TwoFactorRequired));
        Assert.That(Status(await this.VerifyAsync("martin", TestPassword, "000000").ConfigureAwait(false)), Is.EqualTo(AdminLoginStatus.Failed));
        var result = await this.VerifyAsync("martin", TestPassword, TestTotpGenerator.Generate(key!)).ConfigureAwait(false);
        Assert.That(result.Value!.Status, Is.EqualTo(AdminLoginStatus.Succeeded));
    }

    /// <summary>
    /// Failed attempts count for the lockout of the user, like in the panel.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task LockoutAsync()
    {
        await this.CreateUserAsync("martin", withAuthenticator: false).ConfigureAwait(false);
        for (var i = 0; i < 3; i++)
        {
            await this.VerifyAsync("martin", "wrong-password").ConfigureAwait(false);
        }

        Assert.That(Status(await this.VerifyAsync("martin", TestPassword).ConfigureAwait(false)), Is.EqualTo(AdminLoginStatus.LockedOut));
    }

    private static AdminLoginStatus Status(ActionResult<AdminUserVerification> result)
        => ((AdminUserVerification)((UnauthorizedObjectResult)result.Result!).Value!).Status;

    private async Task<ActionResult<AdminUserVerification>> VerifyAsync(string loginName, string password, string? code = null)
    {
        using var scope = this._serviceProvider.CreateScope();
        var controller = new AdminUserController(scope.ServiceProvider.GetRequiredService<AdminLoginService>());
        return await controller.VerifyAsync(new AdminUserCredentials { LoginName = loginName, Password = password, Code = code }).ConfigureAwait(false);
    }

    private async Task<string?> CreateUserAsync(string loginName, bool withAuthenticator)
    {
        using var scope = this._serviceProvider.CreateScope();
        var user = new AdminUser { LoginName = loginName, Roles = AdminRoles.Administrator };
        var result = await scope.ServiceProvider.GetRequiredService<UserManager<AdminUser>>().CreateAsync(user, TestPassword).ConfigureAwait(false);
        Assert.That(result.Succeeded, Is.True);
        if (!withAuthenticator)
        {
            return null;
        }

        var setupService = scope.ServiceProvider.GetRequiredService<AuthenticatorSetupService>();
        var key = (await setupService.BeginSetupAsync(user).ConfigureAwait(false)).SharedKey.Replace(" ", string.Empty);
        Assert.That(await setupService.ConfirmSetupAsync(user, TestTotpGenerator.Generate(key)).ConfigureAwait(false), Is.Not.Null);
        return key;
    }
}
