// <copyright file="ContentCatalogControllerTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Api;

using System.Threading;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Configuration.Quests;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Torvik.ContentStages;
using MUnique.OpenMU.Web.AdminPanel.API;

/// <summary>
/// Tests for the <see cref="ContentCatalogController"/>, which lists the content which can be part of a release.
/// </summary>
[TestFixture]
public class ContentCatalogControllerTests
{
    /// <summary>
    /// The catalog has the content of the game configuration, sorted and with the spanish names.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task GetAsync()
    {
        var persistenceContextProvider = new InMemoryPersistenceContextProvider();
        var unknownPlugInId = Guid.NewGuid();
        using (var context = persistenceContextProvider.CreateNewContext())
        {
            var gameConfiguration = context.CreateNew<GameConfiguration>();
            gameConfiguration.Maps.Add(Map(context, 63, "Vulcanus"));
            gameConfiguration.Maps.Add(Map(context, 0, "Lorencia"));
            gameConfiguration.Items.Add(Item(context, 14, 13, "Jewel of Bless||es=Joya de la Bendición"));
            gameConfiguration.Items.Add(Item(context, 0, 1, "Short Sword||es=Espada Corta"));

            var npc = Monster(context, 257, "Elf Soldier||es=Soldado Elfo", NpcObjectKind.PassiveNpc);
            npc.Quests.Add(Quest(context, 1, 0, "Marlon's Quest"));
            gameConfiguration.Monsters.Add(npc);
            var otherNpc = Monster(context, 229, "Marlon", NpcObjectKind.PassiveNpc);
            otherNpc.Quests.Add(Quest(context, 1, 0, "Marlon's Quest"));
            gameConfiguration.Monsters.Add(otherNpc);
            gameConfiguration.Monsters.Add(Monster(context, 2, "Budge Dragon", NpcObjectKind.Monster));

            gameConfiguration.MiniGameDefinitions.Add(MiniGame(context, MiniGameType.BloodCastle, 1, "Blood Castle 1"));
            gameConfiguration.MiniGameDefinitions.Add(MiniGame(context, MiniGameType.BloodCastle, 1, "Blood Castle 1 (Master)"));
            gameConfiguration.MiniGameDefinitions.Add(MiniGame(context, MiniGameType.DevilSquare, 1, "Devil Square 1"));

            var plugIn = context.CreateNew<PlugInConfiguration>();
            plugIn.TypeId = ContentStagesPlugIn.TypeId;
            gameConfiguration.PlugInConfigurations.Add(plugIn);
            var unknownPlugIn = context.CreateNew<PlugInConfiguration>();
            unknownPlugIn.TypeId = unknownPlugInId;
            gameConfiguration.PlugInConfigurations.Add(unknownPlugIn);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        var catalog = (await new ContentCatalogController(persistenceContextProvider).GetAsync(CancellationToken.None).ConfigureAwait(false)).Value!;

        Assert.That(catalog.Maps.Select(m => m.Name), Is.EqualTo(new[] { "Lorencia", "Vulcanus" }));
        Assert.That(catalog.Items, Is.EqualTo(new[] { new ContentCatalog.Item(0, 1, "Espada Corta"), new ContentCatalog.Item(14, 13, "Joya de la Bendición") }));
        Assert.That(catalog.Npcs, Is.EqualTo(new[] { new ContentCatalog.Npc(229, "Marlon"), new ContentCatalog.Npc(257, "Soldado Elfo") }));
        Assert.That(catalog.Quests.Single(), Is.EqualTo(new ContentCatalog.Quest(1, 0, "Marlon's Quest")));
        Assert.That(catalog.MiniGames.Select(g => (g.Type, g.GameLevel)), Is.EquivalentTo(new[] { (MiniGameType.BloodCastle, (byte)1), (MiniGameType.DevilSquare, (byte)1) }));
        Assert.That(catalog.PlugIns.Single(p => p.Id == ContentStagesPlugIn.TypeId).Name, Is.Not.EqualTo(ContentStagesPlugIn.TypeId.ToString()));
        Assert.That(catalog.PlugIns.Single(p => p.Id == unknownPlugInId), Is.EqualTo(new ContentCatalog.PlugIn(unknownPlugInId, unknownPlugInId.ToString(), null)));
    }

    private static GameMapDefinition Map(Persistence.IContext context, short number, string name)
    {
        var map = context.CreateNew<GameMapDefinition>();
        map.Number = number;
        map.Name = name;
        return map;
    }

    private static ItemDefinition Item(Persistence.IContext context, byte group, short number, string name)
    {
        var item = context.CreateNew<ItemDefinition>();
        item.Group = group;
        item.Number = number;
        item.Name = name;
        return item;
    }

    private static MonsterDefinition Monster(Persistence.IContext context, short number, string designation, NpcObjectKind kind)
    {
        var monster = context.CreateNew<MonsterDefinition>();
        monster.Number = number;
        monster.Designation = designation;
        monster.ObjectKind = kind;
        return monster;
    }

    private static QuestDefinition Quest(Persistence.IContext context, short group, short number, string name)
    {
        var quest = context.CreateNew<QuestDefinition>();
        quest.Group = group;
        quest.Number = number;
        quest.Name = name;
        return quest;
    }

    private static MiniGameDefinition MiniGame(Persistence.IContext context, MiniGameType type, byte level, string name)
    {
        var miniGame = context.CreateNew<MiniGameDefinition>();
        miniGame.Type = type;
        miniGame.GameLevel = level;
        miniGame.Name = name;
        return miniGame;
    }
}
