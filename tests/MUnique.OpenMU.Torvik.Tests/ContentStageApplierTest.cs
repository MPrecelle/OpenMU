// <copyright file="ContentStageApplierTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Raklion;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// Tests the <see cref="ContentStageApplier"/> with the season 6 data.
/// </summary>
[TestFixture]
public class ContentStageApplierTest
{
    private IContext _context = null!;
    private GameConfiguration _configuration = null!;
    private ContentStageApplier _applier = null!;

    /// <summary>
    /// Creates the season 6 data.
    /// </summary>
    /// <returns>The task.</returns>
    [SetUp]
    public async Task SetUpAsync()
    {
        (this._context, this._configuration) = await ContentStageTestData.CreateAsync().ConfigureAwait(false);
        this._applier = new ContentStageApplier(this._context);
    }

    /// <summary>
    /// Disposes the context.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        this._context.Dispose();
    }

    /// <summary>
    /// Tests that the classic stage locks the content of the later stages, and leaves the classic content alone.
    /// </summary>
    [Test]
    public void ClassicLocksLaterContent()
    {
        var kris = this.Item(0, 0);
        var krisDrops = kris.DropsFromMonsters;

        var result = this._applier.Apply(this._configuration, ContentStage.Classic);

        Assert.That(result.CurrentStage, Is.EqualTo(ContentStage.Classic));
        Assert.That(ContentStageApplier.GetConfiguration(this._configuration).CurrentStage, Is.EqualTo(ContentStage.Classic));
        Assert.That(this.Item(0, 26).DropsFromMonsters, Is.False, "Flamberge (socket)");
        Assert.That(this.Item(0, 26).MaximumSockets, Is.Zero, "Flamberge (socket)");
        Assert.That(this.Item(12, 60).DropsFromMonsters, Is.False, "Seed (Fire)");
        Assert.That(kris.DropsFromMonsters, Is.EqualTo(krisDrops), "Kris");
        Assert.That(this._configuration.CharacterClasses.Single(c => c.Number == 24).CanGetCreated, Is.False, "Rage Fighter");
        Assert.That(this._configuration.CharacterClasses.Single(c => c.Number == 20).CanGetCreated, Is.True, "Summoner");
        Assert.That(this._configuration.Monsters.SelectMany(m => m.Quests).Where(q => q.Group == 0 && q.Number >= 4).Select(q => q.MinimumCharacterLevel), Is.All.EqualTo(ContentStageApplier.LockedLevel));
        Assert.That(this.MiniGame(MiniGameType.BloodCastle, 8).MinimumCharacterLevel, Is.EqualTo(ContentStageApplier.LockedLevel));
        Assert.That(this.MiniGame(MiniGameType.Doppelganger, 1).MinimumCharacterLevel, Is.EqualTo(ContentStageApplier.LockedLevel));
        Assert.That(this.MiniGame(MiniGameType.BloodCastle, 7).MinimumCharacterLevel, Is.LessThan(ContentStageApplier.LockedLevel));
        Assert.That(this.PlugIn(typeof(RaklionPlugIn).GUID).IsActive, Is.False);
        Assert.That(this.Spawns(452).Select(a => a.Quantity), Is.All.Zero, "Seed Master");
        Assert.That(this.Spawns(543).Select(a => a.Quantity), Is.All.Zero, "Gens Duprian");
        Assert.That(this.Spawns(240).Select(a => a.Quantity), Is.All.Positive, "Baz The Vault Keeper");
        var quests = this._configuration.Monsters.SelectMany(m => m.Quests).ToList();
        Assert.That(quests.Where(q => q.Group != 0).Select(q => q.MinimumCharacterLevel), Is.All.EqualTo(ContentStageApplier.LockedLevel), "quests of the Elf Soldier");
        Assert.That(quests.Where(q => q.Group == 0 && q.Number < 4).Select(q => q.MinimumCharacterLevel), Is.All.LessThan(ContentStageApplier.LockedLevel), "first and second evolution");
    }

    /// <summary>
    /// Tests that a later stage unlocks the content up to it, and keeps the content after it locked.
    /// </summary>
    [Test]
    public void LaterStageUnlocksItsContent()
    {
        var originalSockets = this.Item(0, 26).MaximumSockets;
        this._applier.Apply(this._configuration, ContentStage.Classic);

        this._applier.Apply(this._configuration, ContentStage.Season4);

        Assert.That(this.Item(0, 26).MaximumSockets, Is.EqualTo(originalSockets), "Flamberge (socket)");
        Assert.That(this.PlugIn(typeof(RaklionPlugIn).GUID).IsActive, Is.True);
        Assert.That(this.MiniGame(MiniGameType.BloodCastle, 8).MinimumCharacterLevel, Is.LessThan(ContentStageApplier.LockedLevel));
        Assert.That(this.MiniGame(MiniGameType.Doppelganger, 1).MinimumCharacterLevel, Is.EqualTo(ContentStageApplier.LockedLevel));
        Assert.That(this._configuration.CharacterClasses.Single(c => c.Number == 24).CanGetCreated, Is.False, "Rage Fighter");
        Assert.That(this.Spawns(452).Select(a => a.Quantity), Is.All.Positive, "Seed Master");
        Assert.That(this.Spawns(540).Select(a => a.Quantity), Is.All.Zero, "Lugard");
    }

    /// <summary>
    /// Tests that applying season 6 after other stages restores all original values and removes the snapshot.
    /// </summary>
    [Test]
    public void SeasonSixRestoresOriginalValues()
    {
        var original = ContentStageTestData.DescribeLockableValues(this._configuration);

        this._applier.Apply(this._configuration, ContentStage.Classic);
        this._applier.Apply(this._configuration, ContentStage.Season5);
        this._applier.Apply(this._configuration, ContentStage.Season3);
        var result = this._applier.Apply(this._configuration, ContentStage.Season6);

        Assert.That(ContentStageTestData.DescribeLockableValues(this._configuration), Is.EqualTo(original));
        Assert.That(result.Snapshot, Is.Null);
    }

    /// <summary>
    /// Tests that applying the same stage again doesn't change anything.
    /// </summary>
    [Test]
    public void ApplyingTwiceChangesNothing()
    {
        this._applier.Apply(this._configuration, ContentStage.Classic);
        var once = ContentStageTestData.DescribeLockableValues(this._configuration);
        var snapshotOnce = this.PlugIn(ContentStagesPlugIn.TypeId).CustomConfiguration;

        this._applier.Apply(this._configuration, ContentStage.Classic);

        Assert.That(ContentStageTestData.DescribeLockableValues(this._configuration), Is.EqualTo(once));
        Assert.That(this.PlugIn(ContentStagesPlugIn.TypeId).CustomConfiguration, Is.EqualTo(snapshotOnce));
    }

    /// <summary>
    /// Tests that the plugin configuration gets created when it doesn't exist yet.
    /// </summary>
    [Test]
    public void CreatesMissingPlugInConfiguration()
    {
        this._configuration.PlugInConfigurations.Remove(this.PlugIn(ContentStagesPlugIn.TypeId));

        this._applier.Apply(this._configuration, ContentStage.Classic);

        Assert.That(this.PlugIn(ContentStagesPlugIn.TypeId).IsActive, Is.True);
        Assert.That(ContentStageApplier.GetConfiguration(this._configuration).CurrentStage, Is.EqualTo(ContentStage.Classic));
    }

    private ItemDefinition Item(byte group, short number) => this._configuration.Items.Single(i => i.Group == group && i.Number == number);

    private MiniGameDefinition MiniGame(MiniGameType type, byte level) => this._configuration.MiniGameDefinitions.Single(g => g.Type == type && g.GameLevel == level);

    private IEnumerable<MonsterSpawnArea> Spawns(short monsterNumber) => this._configuration.Maps.SelectMany(m => m.MonsterSpawns).Where(a => a.MonsterDefinition?.Number == monsterNumber);

    private PlugIns.PlugInConfiguration PlugIn(Guid typeId) => this._configuration.PlugInConfigurations.Single(p => p.TypeId == typeId);
}
