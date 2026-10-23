// <copyright file="AccountControllerTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Api;

using System.ComponentModel.DataAnnotations;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.Web.AdminPanel.API;

/// <summary>
/// Tests for the <see cref="AccountController"/>, which registers accounts and verifies their credentials through the public API.
/// </summary>
[TestFixture]
public class AccountControllerTests
{
    private InMemoryPersistenceContextProvider _persistenceContextProvider = null!;

    private AccountController _controller = null!;

    /// <summary>
    /// Sets up an empty in-memory database and the controller.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        this._persistenceContextProvider = new InMemoryPersistenceContextProvider();
        var gameConfigurationSource = new Mock<IDataSource<GameConfiguration>>();
        gameConfigurationSource
            .Setup(s => s.GetOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GameConfiguration());
        this._controller = new AccountController(this._persistenceContextProvider, gameConfigurationSource.Object, Mock.Of<ILogger<AccountController>>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
    }

    /// <summary>
    /// A valid registration creates an account which can log in with the chosen password.
    /// </summary>
    [Test]
    public async Task RegisterCreatesAccountAsync()
    {
        var result = await this._controller.RegisterAsync(CreateRegistration("newbie", "secret1", "newbie@example.com"), CancellationToken.None).ConfigureAwait(false);

        Assert.That(result, Is.InstanceOf<StatusCodeResult>());
        Assert.That(((StatusCodeResult)result).StatusCode, Is.EqualTo(StatusCodes.Status201Created));

        var account = await this.GetAccountAsync("newbie").ConfigureAwait(false);
        Assert.That(account, Is.Not.Null);
        Assert.That(BCrypt.Net.BCrypt.Verify("secret1", account!.PasswordHash), Is.True);
        Assert.That(account.EMail, Is.EqualTo("newbie@example.com"));
        Assert.That(account.State, Is.EqualTo(AccountState.Normal));
        Assert.That(account.Characters, Is.Empty);
    }

    /// <summary>
    /// The e-mail address is optional.
    /// </summary>
    [Test]
    public async Task RegisterWithoutEMailAsync()
    {
        var result = await this._controller.RegisterAsync(CreateRegistration("newbie", "secret1", null), CancellationToken.None).ConfigureAwait(false);

        Assert.That(((StatusCodeResult)result).StatusCode, Is.EqualTo(StatusCodes.Status201Created));
        Assert.That((await this.GetAccountAsync("newbie").ConfigureAwait(false))!.EMail, Is.Empty);
    }

    /// <summary>
    /// A taken login name is answered with a conflict, and the existing account stays untouched.
    /// </summary>
    [Test]
    public async Task RegisterRejectsTakenLoginNameAsync()
    {
        await this._controller.RegisterAsync(CreateRegistration("newbie", "secret1", null), CancellationToken.None).ConfigureAwait(false);

        var result = await this._controller.RegisterAsync(CreateRegistration("newbie", "other12", null), CancellationToken.None).ConfigureAwait(false);

        Assert.That(result, Is.InstanceOf<ConflictResult>());
        var account = await this.GetAccountAsync("newbie").ConfigureAwait(false);
        Assert.That(BCrypt.Net.BCrypt.Verify("secret1", account!.PasswordHash), Is.True);
    }

    /// <summary>
    /// A registration within the limits of the game client is valid.
    /// </summary>
    [Test]
    public void RegistrationWithinLimitsIsValid()
    {
        Assert.That(GetInvalidMembers(CreateRegistration("abc", "abc", null)), Is.Empty);
        Assert.That(GetInvalidMembers(CreateRegistration("abcdefghij", "pass word with 20 ch", "a@b.c")), Is.Empty);
    }

    /// <summary>
    /// Registrations which the game client couldn't use to log in, or which are incomplete, are invalid.
    /// </summary>
    /// <param name="loginName">The login name.</param>
    /// <param name="password">The password.</param>
    /// <param name="eMail">The e-mail address.</param>
    /// <param name="invalidMember">The member which is expected to be invalid.</param>
    [TestCase("", "secret1", null, nameof(AccountRegistration.LoginName))]
    [TestCase("ab", "secret1", null, nameof(AccountRegistration.LoginName))]
    [TestCase("abcdefghijk", "secret1", null, nameof(AccountRegistration.LoginName))]
    [TestCase("new bie", "secret1", null, nameof(AccountRegistration.LoginName))]
    [TestCase("björn", "secret1", null, nameof(AccountRegistration.LoginName))]
    [TestCase("newbie", "", null, nameof(AccountRegistration.Password))]
    [TestCase("newbie", "ab", null, nameof(AccountRegistration.Password))]
    [TestCase("newbie", "123456789012345678901", null, nameof(AccountRegistration.Password))]
    [TestCase("newbie", "pässwort", null, nameof(AccountRegistration.Password))]
    [TestCase("newbie", "secret1", "not an address", nameof(AccountRegistration.EMail))]
    public void InvalidRegistrationIsRejected(string loginName, string password, string? eMail, string invalidMember)
    {
        Assert.That(GetInvalidMembers(CreateRegistration(loginName, password, eMail)), Is.EqualTo(new[] { invalidMember }));
    }

    /// <summary>
    /// Valid credentials are answered with the login name and state of the account.
    /// </summary>
    [Test]
    public async Task VerifyAcceptsValidCredentialsAsync()
    {
        await this._controller.RegisterAsync(CreateRegistration("newbie", "secret1", null), CancellationToken.None).ConfigureAwait(false);

        var result = await this._controller.VerifyAsync(CreateCredentials("newbie", "secret1"), CancellationToken.None).ConfigureAwait(false);

        Assert.That(result.Value, Is.EqualTo(new AccountVerification("newbie", AccountState.Normal)));
    }

    /// <summary>
    /// The state of a banned account is returned, so that the caller can tell why the login failed.
    /// </summary>
    [Test]
    public async Task VerifyReturnsStateOfBannedAccountAsync()
    {
        using (var context = this._persistenceContextProvider.CreateNewPlayerContext(new GameConfiguration()))
        {
            var account = context.CreateNew<Account>();
            account.LoginName = "banned";
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword("secret1");
            account.State = AccountState.Banned;
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        var result = await this._controller.VerifyAsync(CreateCredentials("banned", "secret1"), CancellationToken.None).ConfigureAwait(false);

        Assert.That(result.Value?.State, Is.EqualTo(AccountState.Banned));
    }

    /// <summary>
    /// The state is serialized by its name, so that callers don't depend on the numeric values.
    /// </summary>
    [Test]
    public void VerificationSerializesStateByName()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new AccountVerification("banned", AccountState.Banned));

        Assert.That(json, Is.EqualTo("""{"LoginName":"banned","State":"Banned"}"""));
    }

    /// <summary>
    /// A wrong password and an unknown login name are both answered with not found.
    /// </summary>
    /// <param name="loginName">The login name.</param>
    /// <param name="password">The password.</param>
    [TestCase("newbie", "wrong12")]
    [TestCase("nobody", "secret1")]
    public async Task VerifyRejectsInvalidCredentialsAsync(string loginName, string password)
    {
        await this._controller.RegisterAsync(CreateRegistration("newbie", "secret1", null), CancellationToken.None).ConfigureAwait(false);

        var result = await this._controller.VerifyAsync(CreateCredentials(loginName, password), CancellationToken.None).ConfigureAwait(false);

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    /// <summary>
    /// Credentials longer than what the game client can send are invalid, but shorter ones are not.
    /// </summary>
    [Test]
    public void CredentialsAreLimitedToTheMaximumLengths()
    {
        Assert.That(GetInvalidMembers(CreateCredentials("ab", "x")), Is.Empty);
        Assert.That(GetInvalidMembers(CreateCredentials("abcdefghijk", "secret1")), Is.EqualTo(new[] { nameof(AccountCredentials.LoginName) }));
        Assert.That(GetInvalidMembers(CreateCredentials("newbie", "123456789012345678901")), Is.EqualTo(new[] { nameof(AccountCredentials.Password) }));
        Assert.That(GetInvalidMembers(CreateCredentials("", "")), Is.EquivalentTo(new[] { nameof(AccountCredentials.LoginName), nameof(AccountCredentials.Password) }));
    }

    private static AccountCredentials CreateCredentials(string loginName, string password)
    {
        return new AccountCredentials { LoginName = loginName, Password = password };
    }

    private static AccountRegistration CreateRegistration(string loginName, string password, string? eMail)
    {
        return new AccountRegistration { LoginName = loginName, Password = password, EMail = eMail };
    }

    private static IEnumerable<string> GetInvalidMembers(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results.SelectMany(r => r.MemberNames).Distinct();
    }

    private async ValueTask<Account?> GetAccountAsync(string loginName)
    {
        using var context = this._persistenceContextProvider.CreateNewPlayerContext(new GameConfiguration());
        return await context.GetAccountByLoginNameAsync(loginName).ConfigureAwait(false);
    }
}
