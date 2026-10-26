// <copyright file="ContentReleaseScheduler.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic.TestActors;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Torvik.Properties;

/// <summary>
/// Releases the scheduled <see cref="ContentRelease"/>s: it warns the players of all game servers with a countdown,
/// releases the content when it's due and restarts the game servers, so that they load the released content.
/// </summary>
/// <remarks>
/// There is one scheduler per process, for all game servers. The warnings are configured by the
/// <see cref="ContentStageConfiguration.CountdownMinutes"/>.
/// </remarks>
public sealed class ContentReleaseScheduler : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(15);

    private readonly IPersistenceContextProvider _persistenceContextProvider;
    private readonly IGameServerInstanceManager _gameServerInstanceManager;
    private readonly IGameServerContextLocator _gameServerContextLocator;
    private readonly ILogger<ContentReleaseScheduler> _logger;
    private ReleaseCountdown? _countdown;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentReleaseScheduler"/> class.
    /// </summary>
    /// <param name="persistenceContextProvider">The persistence context provider.</param>
    /// <param name="gameServerInstanceManager">The manager which restarts the game servers.</param>
    /// <param name="gameServers">The game servers of this process.</param>
    /// <param name="logger">The logger.</param>
    public ContentReleaseScheduler(
        IPersistenceContextProvider persistenceContextProvider,
        IGameServerInstanceManager gameServerInstanceManager,
        IDictionary<int, IGameServer> gameServers,
        ILogger<ContentReleaseScheduler> logger)
    {
        this._persistenceContextProvider = persistenceContextProvider;
        this._gameServerInstanceManager = gameServerInstanceManager;
        this._gameServerContextLocator = new GameServerContextLocator(gameServers);
        this._logger = logger;
    }

    /// <summary>
    /// Sends the warnings which are due, and releases the due releases.
    /// The first call starts the countdown.
    /// </summary>
    /// <param name="now">The current point in time (UTC).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The task.</returns>
    public async Task CheckAsync(DateTime now, CancellationToken cancellationToken)
    {
        this._countdown ??= new ReleaseCountdown(now);
        if (await this.LoadConfigurationAsync(cancellationToken).ConfigureAwait(false) is not { } configuration)
        {
            return;
        }

        var step = this._countdown.Next(configuration, now);
        foreach (var warning in step.Warnings)
        {
            await this.WarnAsync(warning).ConfigureAwait(false);
        }

        if (step.Due.Count > 0)
        {
            await this.ReleaseAsync(step.Due, now, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);
        do
        {
            try
            {
                await this.CheckAsync(DateTime.UtcNow, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // E.g. while the database gets initialized. The next check tries again.
                this._logger.LogError(ex, "Unexpected error when checking the scheduled content releases.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private static LocalizedString GetLocalizedString(string name)
    {
        var neutral = ContentStageResources.ResourceManager.GetString(name, CultureInfo.InvariantCulture) ?? string.Empty;
        var spanish = ContentStageResources.ResourceManager.GetString(name, CultureInfo.GetCultureInfo("es"));
        return new LocalizedString(neutral).WithTranslation(CultureInfo.GetCultureInfo("es"), spanish);
    }

    private async ValueTask<ContentStageConfiguration?> LoadConfigurationAsync(CancellationToken cancellationToken)
    {
        // Only the plugin configurations, because loading the whole game configuration every few seconds is too expensive.
        using var context = this._persistenceContextProvider.CreateNewTypedContext(typeof(PlugInConfiguration), false);
        var plugInConfigurations = await context.GetAsync<PlugInConfiguration>(cancellationToken).ConfigureAwait(false);
        return plugInConfigurations.FirstOrDefault(c => c.TypeId == ContentStagesPlugIn.TypeId)?.GetConfiguration<ContentStageConfiguration>(null);
    }

    private async ValueTask WarnAsync(CountdownWarning warning)
    {
        var names = string.Join(", ", warning.Releases.Select(release => release.Name));
        this._logger.LogInformation("Content releases in {minutes} minutes: {releases}", warning.Minutes, string.Join(", ", warning.Releases.Select(release => release.Id)));
        var message = warning.Minutes == 1
            ? GetLocalizedString(nameof(ContentStageResources.ReleaseCountdownOneMinute))
            : GetLocalizedString(nameof(ContentStageResources.ReleaseCountdown));
        foreach (var (_, context) in this._gameServerContextLocator.Contexts)
        {
            await context.SendGlobalNotificationAsync(message, names, warning.Minutes).ConfigureAwait(false);
        }
    }

    private async ValueTask ReleaseAsync(IReadOnlyList<ContentRelease> due, DateTime now, CancellationToken cancellationToken)
    {
        var released = new List<string>();
        using (await ContentReleaseService.ChangeLock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            using var context = this._persistenceContextProvider.CreateNewContext();
            var gameConfiguration = (await context.GetAsync<GameConfiguration>(cancellationToken).ConfigureAwait(false)).First();
            var service = new ContentReleaseService(context, gameConfiguration);

            // Releases which are due together may require each other, so they are released when their requirements are.
            var pending = due.ToList();
            var errors = new Dictionary<string, IReadOnlyList<ReleaseValidationError>>();
            bool releasedAny;
            do
            {
                releasedAny = false;
                foreach (var release in pending.ToList())
                {
                    errors[release.Id] = service.Release(release.Id, now);
                    if (errors[release.Id].Count == 0)
                    {
                        released.Add(release.Id);
                        pending.Remove(release);
                        releasedAny = true;
                    }
                }
            }
            while (releasedAny && pending.Count > 0);

            foreach (var failed in pending)
            {
                this._logger.LogError("The content release {release} couldn't be released: {errors}", failed.Id, string.Join(", ", errors[failed.Id]));
                this._countdown!.MarkAsFailed(failed);
            }

            if (released.Count == 0)
            {
                return;
            }

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        this._logger.LogInformation("Released the content releases {releases}; restarting the game servers.", string.Join(", ", released));
        await this._gameServerInstanceManager.RestartAllAsync(false).ConfigureAwait(false);
    }
}
