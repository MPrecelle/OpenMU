// <copyright file="AccountVerification.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Text.Json.Serialization;
using MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// The result of <see cref="AccountController.VerifyAsync"/> for valid credentials.
/// </summary>
/// <param name="LoginName">The login name of the account.</param>
/// <param name="State">The state of the account, e.g. to reject banned accounts. It's serialized by its name.</param>
public sealed record AccountVerification(
    string LoginName,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] AccountState State);
