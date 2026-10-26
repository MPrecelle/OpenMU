// <copyright file="ContentStagesPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.Tests;

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Tests;
using MUnique.OpenMU.Torvik.ContentStages;
using BasicMonsterDefinition = MUnique.OpenMU.Persistence.BasicModel.MonsterDefinition;

/// <summary>
/// Tests the runtime locks of the <see cref="ContentStagesPlugIn"/>.
/// </summary>
[TestFixture]
public class ContentStagesPlugInTest
{
    /// <summary>
    /// Tests that the drops of items of later stages are canceled.
    /// </summary>
    /// <param name="group">The item group.</param>
    /// <param name="number">The item number.</param>
    /// <returns><c>true</c>, if the drop was canceled.</returns>
    [TestCase((byte)0, (short)0, ExpectedResult = false)]
    [TestCase((byte)12, (short)36, ExpectedResult = true)]
    [TestCase((byte)12, (short)100, ExpectedResult = true)]
    [TestCase((byte)14, (short)169, ExpectedResult = true)]
    public async Task<bool> LockedItemsDontDropAsync(byte group, short number)
    {
        var (killer, monster) = await CreateKillerAndMonsterAsync().ConfigureAwait(false);
        var plugIn = new ContentStagesPlugIn { Configuration = new ContentStageConfiguration { CurrentStage = ContentStage.Classic } };
        var item = new Item { Definition = new ItemDefinition { Group = group, Number = number } };
        var eventArgs = new CancelEventArgs();

        plugIn.ItemDropping(monster, killer, item, eventArgs);

        return eventArgs.Cancel;
    }

    /// <summary>
    /// Tests that nothing is locked without a configuration.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task NothingIsLockedWithoutConfigurationAsync()
    {
        var (killer, monster) = await CreateKillerAndMonsterAsync().ConfigureAwait(false);
        var plugIn = new ContentStagesPlugIn();
        var item = new Item { Definition = new ItemDefinition { Group = 14, Number = 169 } };
        var eventArgs = new CancelEventArgs();

        plugIn.ItemDropping(monster, killer, item, eventArgs);

        Assert.That(eventArgs.Cancel, Is.False);
    }

    /// <summary>
    /// Tests that the warps to maps of later stages are denied with a message.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task LockedMapCantBeEnteredAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var plugIn = new ContentStagesPlugIn { Configuration = new ContentStageConfiguration { CurrentStage = ContentStage.Season3 } };
        var eventArgs = new CancelEventArgs();

        await plugIn.WarpGateEnteringAsync(player, new ExitGate { Map = new GameMapDefinition { Number = 63 } }, eventArgs).ConfigureAwait(false);

        Assert.That(eventArgs.Cancel, Is.True);
        Mock.Get(player.ViewPlugIns.GetPlugIn<IShowMessagePlugIn>()!).Verify(p => p.ShowMessageAsync(It.IsAny<string>(), MessageType.BlueNormal), Times.Once);
    }

    /// <summary>
    /// Tests that the warps to maps of unlocked stages are allowed.
    /// </summary>
    /// <param name="mapNumber">The map number.</param>
    /// <returns>The task.</returns>
    [TestCase((short)0)]
    [TestCase((short)41)]
    public async Task UnlockedMapCanBeEnteredAsync(short mapNumber)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var plugIn = new ContentStagesPlugIn { Configuration = new ContentStageConfiguration { CurrentStage = ContentStage.Season3 } };
        var eventArgs = new CancelEventArgs();

        await plugIn.WarpGateEnteringAsync(player, new ExitGate { Map = new GameMapDefinition { Number = mapNumber } }, eventArgs).ConfigureAwait(false);

        Assert.That(eventArgs.Cancel, Is.False);
    }

    /// <summary>
    /// Creates a player and a monster which it killed, on Lorencia.
    /// </summary>
    /// <param name="loggerFactory">The logger factory of the game context; <c>null</c> for none.</param>
    /// <returns>The player and the monster.</returns>
    internal static async ValueTask<(Player Killer, Monster Monster)> CreateKillerAndMonsterAsync(ILoggerFactory? loggerFactory = null)
    {
        loggerFactory ??= new NullLoggerFactory();
        var gameConfiguration = PlayerTestHelper.CreateGameConfiguration();
        var mapInitializer = new MapInitializer(gameConfiguration, new NullLogger<MapInitializer>(), NullDropGenerator.Instance, null);
        var gameContext = new GameContext(gameConfiguration, new InMemoryPersistenceContextProvider(), mapInitializer, loggerFactory, new PlugInManager(null, loggerFactory, null, null), NullDropGenerator.Instance, new ConfigurationChangeMediator());
        mapInitializer.PlugInManager = gameContext.PlugInManager;
        mapInitializer.PathFinderPool = gameContext.PathFinderPool;

        var killer = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        var map = await gameContext.GetMapAsync(0).ConfigureAwait(false);
        var definition = new BasicMonsterDefinition { Number = 7, Designation = "Giant", ObjectKind = NpcObjectKind.Monster };
        var spawnArea = new MonsterSpawnArea { MonsterDefinition = definition, GameMap = map!.Definition, X1 = 140, Y1 = 140, X2 = 140, Y2 = 140, Quantity = 1 };
        var monster = new Monster(spawnArea, definition, map, NullDropGenerator.Instance, new Mock<INpcIntelligence>().Object, gameContext.PlugInManager, gameContext.PathFinderPool);
        return (killer, monster);
    }
}
