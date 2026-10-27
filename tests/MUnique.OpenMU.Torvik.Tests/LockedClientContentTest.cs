// <copyright file="LockedClientContentTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.Tests;

using System.IO;
using System.IO.Pipelines;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameServer;
using MUnique.OpenMU.GameServer.RemoteView;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Tests;
using MUnique.OpenMU.Torvik.ContentStages;
using Nito.AsyncEx;

/// <summary>
/// Tests the locked content which is shown to the game client.
/// </summary>
[TestFixture]
public class LockedClientContentTest
{
    /// <summary>
    /// Tests that the defaults lock the gens, the cash shop and the maps of the later releases, in ascending order.
    /// </summary>
    [Test]
    public void DefaultsLockGensCashShopAndMaps()
    {
        var content = ContentStageTestData.CreateConfiguration().CreateLocks().ClientContent;

        Assert.That(content.IsGensLocked, Is.True);
        Assert.That(content.IsCashShopLocked, Is.True);
        Assert.That(content.LockedMaps, Does.Contain((short)41).And.Contain((short)64));
        Assert.That(content.LockedMaps, Is.Ordered.Ascending);
        Assert.That(content.LockedMaps, Does.Not.Contain((short)0));
    }

    /// <summary>
    /// Tests that the released content isn't locked for the client.
    /// </summary>
    [Test]
    public void ReleasedContentIsntLocked()
    {
        var content = ContentStageTestData.CreateConfiguration("gens", "cash-shop", "tercera-evolucion").CreateLocks().ClientContent;

        Assert.That(content.IsGensLocked, Is.False);
        Assert.That(content.IsCashShopLocked, Is.False);
        Assert.That(content.LockedMaps, Does.Not.Contain((short)41).And.Not.Contain((short)42));
    }

    /// <summary>
    /// Tests that nothing is locked without a configuration.
    /// </summary>
    [Test]
    public void NothingIsLockedWithoutConfiguration()
    {
        var content = ContentLocks.None.ClientContent;

        Assert.That(content.IsGensLocked, Is.False);
        Assert.That(content.IsCashShopLocked, Is.False);
        Assert.That(content.LockedMaps, Is.Empty);
    }

    /// <summary>
    /// Tests that the locked content is shown when the player enters the world.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task IsShownWhenEnteringTheWorldAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var plugIn = new ContentStagesPlugIn { Configuration = ContentStageTestData.CreateConfiguration("gens") };

        await plugIn.PlayerStateChangedAsync(player, PlayerState.CharacterSelection, PlayerState.EnteredWorld).ConfigureAwait(false);

        Mock.Get(player.ViewPlugIns.GetPlugIn<IShowLockedContentPlugIn>()!).Verify(
            p => p.ShowLockedContentAsync(It.Is<LockedClientContent>(c => !c.IsGensLocked && c.IsCashShopLocked && c.LockedMaps.Count > 0)),
            Times.Once);
    }

    /// <summary>
    /// Tests that the locked content isn't shown on other state changes.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task IsntShownOnOtherStatesAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var plugIn = new ContentStagesPlugIn { Configuration = ContentStageTestData.CreateConfiguration() };

        await plugIn.PlayerStateChangedAsync(player, PlayerState.Authenticated, PlayerState.CharacterSelection).ConfigureAwait(false);

        Mock.Get(player.ViewPlugIns.GetPlugIn<IShowLockedContentPlugIn>()!).Verify(p => p.ShowLockedContentAsync(It.IsAny<LockedClientContent>()), Times.Never);
    }

    /// <summary>
    /// Tests the bytes of the packet: C2 header, the flags, the count and the map numbers (little endian).
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task PacketHasTheLockedContentAsync()
    {
        var (player, output) = CreateRemotePlayer();
        var plugIn = new ShowLockedContentPlugIn(player);

        await plugIn.ShowLockedContentAsync(new LockedClientContent(true, false, [41, 300])).ConfigureAwait(false);

        Assert.That(output.ToArray(), Is.EqualTo(new byte[] { 0xC2, 0x00, 0x0A, 0xFA, 0x01, 0x02, 41, 0x00, 0x2C, 0x01 }));
    }

    /// <summary>
    /// Tests the packet without locked content.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task PacketWithoutLockedContentAsync()
    {
        var (player, output) = CreateRemotePlayer();
        var plugIn = new ShowLockedContentPlugIn(player);

        await plugIn.ShowLockedContentAsync(ContentLocks.None.ClientContent).ConfigureAwait(false);

        Assert.That(output.ToArray(), Is.EqualTo(new byte[] { 0xC2, 0x00, 0x06, 0xFA, 0x00, 0x00 }));
    }

    private static (RemotePlayer Player, MemoryStream Output) CreateRemotePlayer()
    {
        var manager = new PlugInManager(null, new NullLoggerFactory(), null, null);
        var gameContext = new Mock<IGameServerContext>();
        gameContext.Setup(c => c.PersistenceContextProvider).Returns(new Mock<IPersistenceContextProvider>().Object);
        gameContext.Setup(c => c.Configuration).Returns(new GameConfiguration());
        gameContext.Setup(c => c.PlugInManager).Returns(manager);
        gameContext.Setup(c => c.FeaturePlugIns).Returns(new FeaturePlugInContainer(manager));
        gameContext.Setup(c => c.LoggerFactory).Returns(new NullLoggerFactory());

        var output = new MemoryStream();
        var connection = new Mock<IConnection>();
        connection.SetupGet(c => c.Connected).Returns(true);
        connection.SetupGet(c => c.Output).Returns(PipeWriter.Create(output, new StreamPipeWriterOptions(leaveOpen: true)));
        connection.SetupGet(c => c.OutputLock).Returns(new AsyncLock());
        return (new RemotePlayer(gameContext.Object, connection.Object, default), output);
    }
}
