// <copyright file="ContentRelease.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

using System.Text.Json.Serialization;

/// <summary>
/// A release of content, e.g. a feature, some maps or a set of items. Its content is locked until it's released.
/// </summary>
public class ContentRelease
{
    /// <summary>
    /// Gets or sets the id, a lower case slug like <c>duel-arena</c>. It can't change.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the era of the original game which the content comes from. It only groups the releases.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ContentStage Era { get; set; }

    /// <summary>
    /// Gets or sets the ids of the releases which have to be released before.
    /// </summary>
    public List<string> Requires { get; set; } = new();

    /// <summary>
    /// Gets or sets the content which is locked until the release.
    /// </summary>
    public ReleaseContent Content { get; set; } = new();

    /// <summary>
    /// Gets or sets the point in time (UTC) at which the release is planned; <c>null</c>, if it isn't scheduled.
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Gets or sets the point in time (UTC) at which the content was released; <c>null</c>, if it's still locked.
    /// </summary>
    public DateTime? ReleasedAt { get; set; }

    /// <summary>
    /// Gets a value indicating whether the content was released.
    /// </summary>
    [JsonIgnore]
    public bool IsReleased => this.ReleasedAt is not null;
}
