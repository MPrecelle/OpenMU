// <copyright file="ContentReleaseService.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

using System.Text.Json;
using MUnique.OpenMU.Persistence;
using Nito.AsyncEx;

/// <summary>
/// Changes the <see cref="ContentStageConfiguration.Releases"/> of a game configuration: creates, edits, schedules and releases them.
/// </summary>
/// <remarks>
/// A change is validated as a whole with the <see cref="ContentReleaseValidator"/>. When it's valid, it's applied to the
/// game configuration by the <see cref="ContentStageApplier"/>; otherwise nothing changes. The changes still need to be saved.
/// </remarks>
public sealed class ContentReleaseService
{
    private readonly IContext _context;
    private readonly GameConfiguration _gameConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentReleaseService"/> class.
    /// </summary>
    /// <param name="context">The context in which the game configuration was loaded.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public ContentReleaseService(IContext context, GameConfiguration gameConfiguration)
    {
        this._context = context;
        this._gameConfiguration = gameConfiguration;
        this.Configuration = ContentStageApplier.GetConfiguration(gameConfiguration);
    }

    /// <summary>
    /// Gets the lock which the changes of the releases hold from loading the game configuration until saving it,
    /// so that the API and the <see cref="ContentReleaseScheduler"/> don't overwrite each other's changes.
    /// </summary>
    public static AsyncLock ChangeLock { get; } = new();

    /// <summary>
    /// Gets the current configuration with the releases.
    /// </summary>
    public ContentStageConfiguration Configuration { get; private set; }

    /// <summary>
    /// Creates a release. It's locked and not scheduled.
    /// </summary>
    /// <param name="release">The release.</param>
    /// <returns>The errors; empty, if the release was created.</returns>
    public IReadOnlyList<ReleaseValidationError> Create(ContentRelease release)
    {
        return this.Change(configuration =>
        {
            var created = Clone(release);
            created.ScheduledAt = null;
            created.ReleasedAt = null;
            configuration.Releases.Add(created);
            return null;
        });
    }

    /// <summary>
    /// Changes the name, description, era, requirements and content of a release.
    /// Of a released release, only the name and the description can change.
    /// </summary>
    /// <param name="id">The id of the release.</param>
    /// <param name="changed">The changed release. Its id and points in time are ignored.</param>
    /// <returns>The errors; empty, if the release was changed.</returns>
    public IReadOnlyList<ReleaseValidationError> Update(string id, ContentRelease changed)
    {
        return this.Change(configuration =>
        {
            var release = GetRelease(configuration, id);
            if (release.IsReleased && !HaveSameContent(release, changed))
            {
                return new(ReleaseValidationErrorCode.AlreadyReleased, id);
            }

            var copy = Clone(changed);
            release.Name = copy.Name;
            release.Description = copy.Description;
            release.Era = copy.Era;
            release.Requires = copy.Requires;
            release.Content = copy.Content;
            return null;
        });
    }

    /// <summary>
    /// Deletes a release which isn't released.
    /// </summary>
    /// <param name="id">The id of the release.</param>
    /// <returns>The errors; empty, if the release was deleted.</returns>
    public IReadOnlyList<ReleaseValidationError> Delete(string id)
    {
        return this.Change(configuration =>
        {
            var release = GetRelease(configuration, id);
            if (release.IsReleased)
            {
                return new(ReleaseValidationErrorCode.AlreadyReleased, id);
            }

            configuration.Releases.Remove(release);
            return null;
        });
    }

    /// <summary>
    /// Schedules a release which isn't released, or removes its schedule.
    /// </summary>
    /// <param name="id">The id of the release.</param>
    /// <param name="scheduledAt">The point in time (UTC) of the release; <c>null</c> to remove the schedule.</param>
    /// <param name="now">The current point in time (UTC).</param>
    /// <returns>The errors; empty, if the schedule was changed.</returns>
    public IReadOnlyList<ReleaseValidationError> Schedule(string id, DateTime? scheduledAt, DateTime now)
    {
        return this.Change(configuration =>
        {
            var release = GetRelease(configuration, id);
            if (release.IsReleased)
            {
                return new(ReleaseValidationErrorCode.AlreadyReleased, id);
            }

            if (scheduledAt <= now)
            {
                return new(ReleaseValidationErrorCode.ScheduledInThePast, id);
            }

            release.ScheduledAt = scheduledAt;
            return null;
        });
    }

    /// <summary>
    /// Schedules a release which isn't released at the end of the countdown of <see cref="ContentStageConfiguration.ReleaseNowMinutes"/>.
    /// </summary>
    /// <param name="id">The id of the release.</param>
    /// <param name="now">The current point in time (UTC).</param>
    /// <returns>The errors; empty, if the release was scheduled.</returns>
    public IReadOnlyList<ReleaseValidationError> ReleaseAfterCountdown(string id, DateTime now)
        => this.Schedule(id, now.AddMinutes(Math.Max(this.Configuration.ReleaseNowMinutes, 1)), now);

    /// <summary>
    /// Releases the content of a release.
    /// </summary>
    /// <param name="id">The id of the release.</param>
    /// <param name="now">The current point in time (UTC).</param>
    /// <returns>The errors; empty, if the release was released.</returns>
    public IReadOnlyList<ReleaseValidationError> Release(string id, DateTime now)
    {
        return this.Change(configuration =>
        {
            var release = GetRelease(configuration, id);
            if (release.IsReleased)
            {
                return new(ReleaseValidationErrorCode.AlreadyReleased, id);
            }

            release.ReleasedAt = now;
            release.ScheduledAt = null;
            return null;
        });
    }

    /// <summary>
    /// Locks the content of a released release again.
    /// </summary>
    /// <remarks>
    /// The players may already have its items or be on its maps, so it's meant for tests and emergencies.
    /// </remarks>
    /// <param name="id">The id of the release.</param>
    /// <returns>The errors; empty, if the release was reverted.</returns>
    public IReadOnlyList<ReleaseValidationError> Revert(string id)
    {
        return this.Change(configuration =>
        {
            var release = GetRelease(configuration, id);
            if (!release.IsReleased)
            {
                return new(ReleaseValidationErrorCode.NotReleased, id);
            }

            release.ReleasedAt = null;
            return null;
        });
    }

    /// <summary>
    /// Adds the <see cref="DefaultReleases"/> whose ids don't exist yet. They are locked.
    /// </summary>
    /// <returns>The errors; empty, if the missing releases were added.</returns>
    public IReadOnlyList<ReleaseValidationError> AddMissingDefaults()
    {
        return this.Change(configuration =>
        {
            configuration.Releases.AddRange(DefaultReleases.Create().Where(release => configuration.FindRelease(release.Id) is null));
            return null;
        });
    }

    private static ContentRelease GetRelease(ContentStageConfiguration configuration, string id)
        => configuration.FindRelease(id) ?? throw new ArgumentException($"There is no release with the id '{id}'.", nameof(id));

    private static bool HaveSameContent(ContentRelease release, ContentRelease other)
        => release.Era == other.Era
           && release.Requires.SequenceEqual(other.Requires)
           && JsonSerializer.Serialize(release.Content) == JsonSerializer.Serialize(other.Content);

    private static T Clone<T>(T value)
        => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;

    private IReadOnlyList<ReleaseValidationError> Change(Func<ContentStageConfiguration, ReleaseValidationError?> change)
    {
        var configuration = Clone(this.Configuration);
        if (change(configuration) is { } error)
        {
            return [error];
        }

        var errors = ContentReleaseValidator.Validate(configuration.Releases, this._gameConfiguration);
        if (errors.Count > 0)
        {
            return errors;
        }

        new ContentStageApplier(this._context).Apply(this._gameConfiguration, configuration);
        this.Configuration = configuration;
        return [];
    }
}
