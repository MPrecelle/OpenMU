// <copyright file="DropLogPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.Tests;

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Moq;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// Tests the log entries of the drops, which are used to verify the content stages in the game.
/// </summary>
[TestFixture]
public class DropLogPlugInTest
{
    /// <summary>
    /// Tests that the drops of monsters are logged with the item group and number.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task DropIsLoggedAsync()
    {
        var logger = CreateLoggerMock(out var loggerFactory);
        var (killer, monster) = await ContentStagesPlugInTest.CreateKillerAndMonsterAsync(loggerFactory).ConfigureAwait(false);
        var item = new Item { Definition = new ItemDefinition { Name = "Jewel of Bless", Group = 14, Number = 13 } };
        await using var droppedItem = new DroppedItem(item, new Point(140, 140), monster.CurrentMap, null, null);

        await new DropLogPlugIn().MonsterItemDroppedAsync(monster, killer, droppedItem).ConfigureAwait(false);

        VerifyLogged(logger, "Drop: Jewel of Bless (14:13) from Giant");
    }

    /// <summary>
    /// Tests that the drops which the <see cref="ContentStagesPlugIn"/> cancels are logged.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task BlockedDropIsLoggedAsync()
    {
        var logger = CreateLoggerMock(out var loggerFactory);
        var (killer, monster) = await ContentStagesPlugInTest.CreateKillerAndMonsterAsync(loggerFactory).ConfigureAwait(false);
        var plugIn = new ContentStagesPlugIn { Configuration = new ContentStageConfiguration { CurrentStage = ContentStage.Classic } };
        var item = new Item { Definition = new ItemDefinition { Name = "Seed", Group = 12, Number = 60 } };

        plugIn.ItemDropping(monster, killer, item, new CancelEventArgs());

        VerifyLogged(logger, "Drop blocked by the content stage Classic: Seed (12:60) from Giant");
    }

    private static Mock<ILogger> CreateLoggerMock(out ILoggerFactory loggerFactory)
    {
        var logger = new Mock<ILogger>();
        logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var factory = new Mock<ILoggerFactory>();
        factory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);
        loggerFactory = factory.Object;
        return logger;
    }

    private static void VerifyLogged(Mock<ILogger> logger, string expectedStart)
    {
        logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.StartsWith(expectedStart, StringComparison.Ordinal)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
