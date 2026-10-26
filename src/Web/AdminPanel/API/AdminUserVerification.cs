// <copyright file="AdminUserVerification.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Text.Json.Serialization;
using MUnique.OpenMU.Web.AdminPanel.Auth;

/// <summary>
/// Torvik MU: the result of <see cref="AdminUserController.VerifyAsync"/>.
/// </summary>
/// <param name="Status">The status of the login.</param>
/// <param name="LoginName">The login name of the user, when the login succeeded.</param>
/// <param name="Roles">The effective roles of the user, when the login succeeded.</param>
public sealed record AdminUserVerification(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] AdminLoginStatus Status,
    string? LoginName = null,
    IReadOnlyList<string>? Roles = null);
