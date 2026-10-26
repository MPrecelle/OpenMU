// <copyright file="AdminUserCredentials.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Torvik MU: the credentials of an admin panel user, as sent to <see cref="AdminUserController.VerifyAsync"/>.
/// </summary>
public class AdminUserCredentials
{
    /// <summary>
    /// Gets or sets the login name.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string LoginName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the code of the authenticator app, or a recovery code. It's needed when the user has a second factor.
    /// </summary>
    [MaxLength(64)]
    public string? Code { get; set; }
}
