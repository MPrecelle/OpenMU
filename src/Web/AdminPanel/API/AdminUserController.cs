// <copyright file="AdminUserController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MUnique.OpenMU.Web.AdminPanel.Auth;

/// <summary>
/// Torvik MU: lets an external site (our web) log in the users of the admin panel, with the same checks as the panel:
/// password, second factor and lockout.
/// </summary>
[ApiController]
[Route("api/admin-users")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.ApiSchemes, Policy = AdminPolicies.Operator)]
public class AdminUserController : ControllerBase
{
    private readonly AdminLoginService _loginService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminUserController"/> class.
    /// </summary>
    /// <param name="loginService">The login service of the admin panel.</param>
    public AdminUserController(AdminLoginService loginService)
    {
        this._loginService = loginService;
    }

    /// <summary>
    /// Verifies the credentials of an admin panel user.
    /// </summary>
    /// <param name="credentials">The login name, the password and, for users with a second factor, the code.</param>
    /// <returns>
    /// <c>200</c> with the login name and the effective roles when the login succeeded; <c>401</c> with the status
    /// (<c>Failed</c>, <c>LockedOut</c> or <c>TwoFactorRequired</c>) otherwise.
    /// </returns>
    /// <remarks>
    /// The caller has to protect its form against guessing passwords (a rate limit per visitor). Failed attempts count
    /// for the lockout of the user, like in the panel.
    /// </remarks>
    [HttpPost("verify")]
    public async Task<ActionResult<AdminUserVerification>> VerifyAsync([FromBody] AdminUserCredentials credentials)
    {
        var result = await this._loginService.CheckPasswordAsync(credentials.LoginName, credentials.Password, false).ConfigureAwait(false);
        if (result.Status == AdminLoginStatus.TwoFactorRequired && !string.IsNullOrWhiteSpace(credentials.Code))
        {
            var isRecoveryCode = credentials.Code.Replace(" ", string.Empty).Replace("-", string.Empty).Length > 6;
            result = await this._loginService.CheckTwoFactorAsync(credentials.Code, isRecoveryCode).ConfigureAwait(false);
        }

        if (result.Status != AdminLoginStatus.Succeeded)
        {
            return this.Unauthorized(new AdminUserVerification(result.Status));
        }

        var claims = result.Claims ?? [];
        return new AdminUserVerification(
            result.Status,
            claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
            claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList());
    }
}
