// <copyright file="ContentReleaseSchedulerTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.Tests;

using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// Tests the <see cref="ContentReleaseScheduler"/> with an in-memory database.
/// </summary>
[TestFixture]
public class ContentReleaseSchedulerTest
{
    private static readonly DateTime Start = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

    private InMemoryPersistenceContextProvider _persistenceContextProvider = null!;
    private Mock<IGameServerInstanceManager> _instanceManager = null!;
    private Mock<IGameServerContext> _gameContext = null!;
    private ContentReleaseScheduler _scheduler = null!;

    /// <summary>
    /// Creates the scheduler with one game server.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        this._persistenceContextProvider = new InMemoryPersistenceContextProvider();
        this._instanceManager = new Mock<IGameServerInstanceManager>();
        this._gameContext = new Mock<IGameServerContext>();
        var gameServer = new Mock<IGameServer>();
        gameServer.As<IGameServerContextProvider>().Setup(s => s.Context).Returns(this._gameContext.Object);
        var gameServers = new Dictionary<int, IGameServer> { [0] = gameServer.Object };
        this._scheduler = new ContentReleaseScheduler(this._persistenceContextProvider, this._instanceManager.Object, gameServers, NullLogger<ContentReleaseScheduler>.Instance);
    }

    /// <summary>
    /// Tests that the players are warned in their language, and that the release is released and the game servers restarted when it's due.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task WarnsAndReleasesAsync()
    {
        await this.StoreAsync(Release("vulcanus", Start.AddHours(1))).ConfigureAwait(false);
        LocalizedString? message = null;
        this._gameContext.Setup(c => c.SendGlobalNotificationAsync(It.IsAny<LocalizedString>(), It.IsAny<object?[]>()))
            .Callback<LocalizedString, object?[]>((m, _) => message = m);

        await this._scheduler.CheckAsync(Start, CancellationToken.None).ConfigureAwait(false);
        this._instanceManager.Verify(m => m.RestartAllAsync(It.IsAny<bool>()), Times.Never);
        await this._scheduler.CheckAsync(Start.AddHours(1), CancellationToken.None).ConfigureAwait(false);

        this._gameContext.Verify(c => c.SendGlobalNotificationAsync(It.IsAny<LocalizedString>(), It.Is<object?[]>(a => a.SequenceEqual(new object?[] { "Vulcanus", 60 }))), Times.Once);
        Assert.That(message!.Value.GetTranslation(CultureInfo.GetCultureInfo("es")), Does.StartWith("Lanzamiento de {0} en {1} minutos."));
        Assert.That(message.Value.GetTranslation(CultureInfo.InvariantCulture), Does.StartWith("Release of {0} in {1} minutes."));
        this._instanceManager.Verify(m => m.RestartAllAsync(false), Times.Once);
        Assert.That((await this.LoadAsync().ConfigureAwait(false)).FindRelease("vulcanus")!.ReleasedAt, Is.EqualTo(Start.AddHours(1)));
    }

    /// <summary>
    /// Tests that releases which are due together are released in the order of their requirements, with one restart.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task ReleasesRequirementsFirstAsync()
    {
        var vulcanus = Release("vulcanus", Start.AddHours(1));
        vulcanus.Requires.Add("gens");
        await this.StoreAsync(vulcanus, Release("gens", Start.AddHours(1))).ConfigureAwait(false);

        await this._scheduler.CheckAsync(Start, CancellationToken.None).ConfigureAwait(false);
        await this._scheduler.CheckAsync(Start.AddHours(1), CancellationToken.None).ConfigureAwait(false);

        var configuration = await this.LoadAsync().ConfigureAwait(false);
        Assert.That(configuration.Releases.Select(r => r.IsReleased), Is.All.True);
        this._instanceManager.Verify(m => m.RestartAllAsync(false), Times.Once);
    }

    /// <summary>
    /// Tests that a due release which can't be released doesn't restart the game servers, and isn't tried again.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task FailedReleaseDoesNotRestartAsync()
    {
        var vulcanus = Release("vulcanus", Start.AddHours(1));
        vulcanus.Requires.Add("gens");
        await this.StoreAsync(vulcanus, Release("gens", null)).ConfigureAwait(false);

        await this._scheduler.CheckAsync(Start, CancellationToken.None).ConfigureAwait(false);
        await this._scheduler.CheckAsync(Start.AddHours(1), CancellationToken.None).ConfigureAwait(false);
        await this._scheduler.CheckAsync(Start.AddHours(1).AddSeconds(15), CancellationToken.None).ConfigureAwait(false);

        Assert.That((await this.LoadAsync().ConfigureAwait(false)).FindRelease("vulcanus")!.IsReleased, Is.False);
        this._instanceManager.Verify(m => m.RestartAllAsync(It.IsAny<bool>()), Times.Never);
    }

    /// <summary>
    /// Tests that nothing happens without a game configuration, e.g. before the database is initialized.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task WithoutConfigurationNothingHappensAsync()
    {
        await this._scheduler.CheckAsync(Start, CancellationToken.None).ConfigureAwait(false);

        this._instanceManager.Verify(m => m.RestartAllAsync(It.IsAny<bool>()), Times.Never);
    }

    private static ContentRelease Release(string id, DateTime? scheduledAt)
        => new() { Id = id, Name = char.ToUpperInvariant(id[0]) + id[1..], ScheduledAt = scheduledAt };

    private async Task StoreAsync(params ContentRelease[] releases)
    {
        using var context = this._persistenceContextProvider.CreateNewContext();
        var gameConfiguration = context.CreateNew<GameConfiguration>();
        var plugInConfiguration = context.CreateNew<PlugInConfiguration>();
        plugInConfiguration.TypeId = ContentStagesPlugIn.TypeId;
        plugInConfiguration.IsActive = true;
        var configuration = new ContentStageConfiguration();
        configuration.Releases.AddRange(releases);
        plugInConfiguration.SetConfiguration(configuration, null);
        gameConfiguration.PlugInConfigurations.Add(plugInConfiguration);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    private async Task<ContentStageConfiguration> LoadAsync()
    {
        using var context = this._persistenceContextProvider.CreateNewContext();
        var gameConfiguration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        return ContentStageApplier.GetConfiguration(gameConfiguration);
    }
}
