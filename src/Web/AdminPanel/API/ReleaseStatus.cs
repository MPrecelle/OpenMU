// <copyright file="ReleaseStatus.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

/// <summary>
/// Torvik MU: the status of a content release.
/// </summary>
public enum ReleaseStatus
{
    /// <summary>
    /// The content is locked and the release isn't scheduled.
    /// </summary>
    Locked,

    /// <summary>
    /// The content is locked until the scheduled point in time.
    /// </summary>
    Scheduled,

    /// <summary>
    /// The content is released.
    /// </summary>
    Released,
}
