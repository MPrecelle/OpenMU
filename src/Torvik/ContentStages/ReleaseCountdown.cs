// <copyright file="ReleaseCountdown.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// Decides when the players get warned about the scheduled releases, and when they are due.
/// It remembers the sent warnings, so each one is sent once.
/// </summary>
/// <remarks>
/// A release is never due before the <see cref="ContentStageConfiguration.ReleaseNowMinutes"/> after the start of the countdown,
/// so that the players are warned about the releases which became due while the server was down.
/// </remarks>
public sealed class ReleaseCountdown
{
    private readonly DateTime _startedAt;
    private readonly HashSet<(string ReleaseId, DateTime ReleaseAt, int Minutes)> _sentWarnings = new();
    private readonly HashSet<(string ReleaseId, DateTime ScheduledAt)> _failed = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseCountdown"/> class.
    /// </summary>
    /// <param name="startedAt">The point in time (UTC) when the server started.</param>
    public ReleaseCountdown(DateTime startedAt)
    {
        this._startedAt = startedAt;
    }

    /// <summary>
    /// Determines the warnings which have to be sent now, and the releases which are due.
    /// </summary>
    /// <param name="configuration">The configuration with the releases.</param>
    /// <param name="now">The current point in time (UTC).</param>
    /// <returns>The warnings and the due releases.</returns>
    public CountdownStep Next(ContentStageConfiguration configuration, DateTime now)
    {
        var earliest = this._startedAt.AddMinutes(configuration.ReleaseNowMinutes);
        var scheduled = configuration.Releases
            .Where(release => !release.IsReleased && release.ScheduledAt is { } at && !this._failed.Contains((release.Id, at)))
            .Select(release => (Release: release, At: release.ScheduledAt!.Value > earliest ? release.ScheduledAt.Value : earliest))
            .OrderBy(item => item.At)
            .ToList();

        var due = scheduled.Where(item => item.At <= now).Select(item => item.Release).ToList();
        var warnings = new List<CountdownWarning>();
        foreach (var group in scheduled.Where(item => item.At > now).GroupBy(item => item.At))
        {
            var remaining = group.Key - now;
            var releases = new List<ContentRelease>();
            foreach (var (release, releaseAt) in group)
            {
                // When several warnings are reached at once, e.g. because the release was scheduled shortly before, only one is sent.
                var hasNewWarning = false;
                foreach (var minutes in configuration.CountdownMinutes.Where(minutes => remaining <= TimeSpan.FromMinutes(minutes)))
                {
                    hasNewWarning |= this._sentWarnings.Add((release.Id, releaseAt, minutes));
                }

                if (hasNewWarning)
                {
                    releases.Add(release);
                }
            }

            if (releases.Count > 0)
            {
                warnings.Add(new CountdownWarning((int)Math.Ceiling(remaining.TotalMinutes), releases));
            }
        }

        return new CountdownStep(warnings, due);
    }

    /// <summary>
    /// Remembers that a release couldn't be released, so that it isn't due again until it's scheduled again.
    /// </summary>
    /// <param name="release">The release.</param>
    public void MarkAsFailed(ContentRelease release)
    {
        if (release.ScheduledAt is { } scheduledAt)
        {
            this._failed.Add((release.Id, scheduledAt));
        }
    }
}
