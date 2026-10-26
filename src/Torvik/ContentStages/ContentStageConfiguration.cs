// <copyright file="ContentStageConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

using System.Text.Json.Serialization;

/// <summary>
/// The configuration of the <see cref="ContentStagesPlugIn"/>: the releases of content and the original values of the locked content.
/// </summary>
public class ContentStageConfiguration : IJsonOnDeserialized
{
    /// <summary>
    /// Gets or sets the releases. The content of the releases which aren't released yet is locked.
    /// </summary>
    /// <remarks>
    /// Change them through the <see cref="ContentReleaseService"/>, which also changes the configuration data accordingly.
    /// </remarks>
    public List<ContentRelease> Releases { get; set; } = new();

    /// <summary>
    /// Gets or sets the original values of the configuration data which is changed when content gets locked.
    /// It's <c>null</c> when nothing is locked.
    /// </summary>
    public ContentStageSnapshot? Snapshot { get; set; }

    /// <summary>
    /// Gets or sets the minutes before a release at which the players are warned that it comes and that the server restarts.
    /// </summary>
    public List<int> CountdownMinutes { get; set; } = [60, 15, 5, 1];

    /// <summary>
    /// Gets or sets the minutes of the countdown of a release which is released now, through the API.
    /// After a start of the server, releases which are due wait for this countdown, too.
    /// </summary>
    public int ReleaseNowMinutes { get; set; } = 5;

    /// <summary>
    /// Gets or sets the stage of a configuration which was stored before the releases existed.
    /// It's only read, and replaced by the <see cref="DefaultReleases"/> when the configuration is loaded.
    /// </summary>
    [JsonPropertyName("CurrentStage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentStage? LegacyStage { get; set; }

    /// <summary>
    /// Creates the locks of the content of the releases which aren't released yet.
    /// </summary>
    /// <returns>The locks.</returns>
    public ContentLocks CreateLocks() => new(this.Releases.Where(release => !release.IsReleased));

    /// <summary>
    /// Finds the release with the id.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>The release; <c>null</c>, if there is none with this id.</returns>
    public ContentRelease? FindRelease(string id) => this.Releases.FirstOrDefault(release => release.Id == id);

    /// <inheritdoc />
    void IJsonOnDeserialized.OnDeserialized()
    {
        if (this.LegacyStage is not { } stage)
        {
            return;
        }

        if (this.Releases.Count == 0)
        {
            // The release time of the content of the reached stage isn't known.
            this.Releases = DefaultReleases.Create();
            this.Releases.Where(release => release.Era <= stage).ToList().ForEach(release => release.ReleasedAt = DateTime.UnixEpoch);
        }

        this.LegacyStage = null;
    }
}
