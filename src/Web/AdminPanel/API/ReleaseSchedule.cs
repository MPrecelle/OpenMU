// <copyright file="ReleaseSchedule.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

/// <summary>
/// Torvik MU: the point in time at which a content release is planned.
/// </summary>
/// <param name="ScheduledAt">The point in time, with its offset, e.g. <c>2027-03-01T20:00:00-03:00</c>.</param>
public sealed record ReleaseSchedule(DateTimeOffset ScheduledAt);
