// <copyright file="AccountCredentials.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// The login name and password of an account, as sent to <see cref="AccountController.VerifyAsync"/>.
/// </summary>
/// <remarks>
/// Only the maximum lengths are checked, so that an existing account which doesn't match the
/// rules of <see cref="AccountRegistration"/> can still log in.
/// </remarks>
public class AccountCredentials
{
    /// <summary>
    /// Gets or sets the login name.
    /// </summary>
    [Required]
    [MaxLength(AccountRegistration.LoginNameMaximumLength)]
    public string LoginName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    [Required]
    [MaxLength(AccountRegistration.PasswordMaximumLength)]
    public string Password { get; set; } = string.Empty;
}
