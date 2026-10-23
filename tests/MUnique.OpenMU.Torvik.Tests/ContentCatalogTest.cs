// <copyright file="ContentCatalogTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// Tests the <see cref="ContentCatalog"/> against the season 6 data.
/// </summary>
[TestFixture]
public class ContentCatalogTest
{
    private IContext _context = null!;
    private GameConfiguration _configuration = null!;

    /// <summary>
    /// Creates the season 6 data.
    /// </summary>
    /// <returns>The task.</returns>
    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        (this._context, this._configuration) = await ContentStageTestData.CreateAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the context.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
        this._context.Dispose();
    }

    /// <summary>
    /// Tests that the maps, character classes, quests and plugins of the catalog exist.
    /// </summary>
    [Test]
    public void CatalogEntriesExist()
    {
        Assert.That(ContentCatalog.Maps.Keys, Is.SubsetOf(this._configuration.Maps.Select(m => m.Number)));
        Assert.That(ContentCatalog.CharacterClasses.Keys, Is.SubsetOf(this._configuration.CharacterClasses.Select(c => c.Number)));
        Assert.That(ContentCatalog.LegacyQuests.Keys, Is.SubsetOf(this._configuration.Monsters.SelectMany(m => m.Quests).Where(q => q.Group == 0).Select(q => q.Number)));
        Assert.That(ContentCatalog.PlugIns.Keys, Is.SubsetOf(this._configuration.PlugInConfigurations.Select(p => p.TypeId)));
    }

    /// <summary>
    /// Tests that every item range of the catalog contains at least one item.
    /// </summary>
    [Test]
    public void ItemRangesAreNotEmpty()
    {
        var emptyRanges = ContentCatalog.Items.Where(range => !this._configuration.Items.Any(range.Contains));
        Assert.That(emptyRanges, Is.Empty);
    }

    /// <summary>
    /// Tests that the items with sockets, which came with season 4, aren't classic.
    /// </summary>
    [Test]
    public void SocketItemsAreFromSeasonFour()
    {
        var classicSocketItems = this._configuration.Items
            .Where(i => i.MaximumSockets > 0 && ContentCatalog.GetStage(i) < ContentStage.Season4)
            .Select(i => i.ToString());
        Assert.That(classicSocketItems, Is.Empty);
    }

    /// <summary>
    /// Tests the stages of some mini games.
    /// </summary>
    /// <param name="type">The type of the mini game.</param>
    /// <param name="level">The level of the mini game.</param>
    /// <returns>The stage of the mini game.</returns>
    [TestCase(MiniGameType.BloodCastle, 7, ExpectedResult = ContentStage.Classic)]
    [TestCase(MiniGameType.BloodCastle, 8, ExpectedResult = ContentStage.Season3)]
    [TestCase(MiniGameType.DevilSquare, 7, ExpectedResult = ContentStage.Season3)]
    [TestCase(MiniGameType.ChaosCastle, 7, ExpectedResult = ContentStage.Season3)]
    [TestCase(MiniGameType.Kanturu, 1, ExpectedResult = ContentStage.Classic)]
    [TestCase(MiniGameType.Doppelganger, 1, ExpectedResult = ContentStage.Season5)]
    [TestCase(MiniGameType.ImperialGuardian, 1, ExpectedResult = ContentStage.Season5)]
    public ContentStage MiniGameStages(MiniGameType type, int level)
    {
        return ContentCatalog.GetStage(this._configuration.MiniGameDefinitions.Single(g => g.Type == type && g.GameLevel == level));
    }
}
