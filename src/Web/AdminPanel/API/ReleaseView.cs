// <copyright file="ReleaseView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Text.Json.Serialization;
using MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// Torvik MU: a content release, as <see cref="ContentReleaseController"/> returns it.
/// </summary>
/// <param name="Id">The id.</param>
/// <param name="Name">The name.</param>
/// <param name="Description">The description.</param>
/// <param name="Era">The era of the original game which the content comes from.</param>
/// <param name="Status">The status.</param>
/// <param name="ScheduledAt">The point in time (UTC) at which the release is planned.</param>
/// <param name="ReleasedAt">The point in time (UTC) at which the content was released.</param>
/// <param name="Requires">The ids of the releases which have to be released before.</param>
/// <param name="Content">The content which is locked until the release.</param>
public sealed record ReleaseView(
    string Id,
    string Name,
    string? Description,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ContentStage Era,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ReleaseStatus Status,
    DateTime? ScheduledAt,
    DateTime? ReleasedAt,
    IReadOnlyList<string> Requires,
    ReleaseContent Content)
{
    /// <summary>
    /// Creates the view of a release.
    /// </summary>
    /// <param name="release">The release.</param>
    /// <returns>The view.</returns>
    public static ReleaseView From(ContentRelease release)
    {
        var status = release.IsReleased ? ReleaseStatus.Released
            : release.ScheduledAt is not null ? ReleaseStatus.Scheduled
            : ReleaseStatus.Locked;
        return new(release.Id, release.Name, release.Description, release.Era, status, release.ScheduledAt, release.ReleasedAt, release.Requires, release.Content);
    }
}
