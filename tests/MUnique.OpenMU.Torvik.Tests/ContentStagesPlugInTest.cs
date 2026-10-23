// <copyright file="ContentStagesPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.Tests;

using System.ComponentModel;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Tests;
using MUnique.OpenMU.Torvik.ContentStages;

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
    public bool LockedItemsDontDrop(byte group, short number)
    {
        var plugIn = new ContentStagesPlugIn { Configuration = new ContentStageConfiguration { CurrentStage = ContentStage.Classic } };
        var item = new Item { Definition = new ItemDefinition { Group = group, Number = number } };
        var eventArgs = new CancelEventArgs();

        plugIn.ItemDropping(null!, null!, item, eventArgs);

        return eventArgs.Cancel;
    }

    /// <summary>
    /// Tests that nothing is locked without a configuration.
    /// </summary>
    [Test]
    public void NothingIsLockedWithoutConfiguration()
    {
        var plugIn = new ContentStagesPlugIn();
        var item = new Item { Definition = new ItemDefinition { Group = 14, Number = 169 } };
        var eventArgs = new CancelEventArgs();

        plugIn.ItemDropping(null!, null!, item, eventArgs);

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
}
