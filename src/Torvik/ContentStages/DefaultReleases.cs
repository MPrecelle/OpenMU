// <copyright file="DefaultReleases.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.Gens;
using MUnique.OpenMU.GameLogic.PlugIns.UnlockCharacterClass;
using MUnique.OpenMU.GameLogic.Raklion;
using ItemRange = MUnique.OpenMU.Torvik.ContentStages.ReleaseContent.ItemRange;
using MiniGameReference = MUnique.OpenMU.Torvik.ContentStages.ReleaseContent.MiniGameReference;
using QuestReference = MUnique.OpenMU.Torvik.ContentStages.ReleaseContent.QuestReference;

/// <summary>
/// The releases with which a server starts with the classic content: everything after season 3 episode 1 of the
/// season 6 data, split into features, maps and sets of items. They are locked until they get released.
/// </summary>
/// <remarks>
/// The data is the starting point; the releases can be changed through the API afterwards.
/// </remarks>
public static class DefaultReleases
{
    private static readonly Guid DuelGroupHandlerPlugInId = new("88A8E1DB-C3EC-45A0-98F9-E9DA6F17373C");

    /// <summary>
    /// Creates new instances of the default releases, all locked.
    /// </summary>
    /// <returns>The default releases.</returns>
    public static List<ContentRelease> Create() =>
    [
        Release("tercera-evolucion", "Tercera evolución", ContentStage.Season3, content =>
        {
            content.Maps.AddRange([41, 42]); // Barracks of Balgass, Balgass Refuge
            content.Quests.AddRange([new QuestReference(0, 4), new QuestReference(0, 5), new QuestReference(0, 6)]); // Apostle Devin
            content.MiniGames.AddRange([
                new MiniGameReference(MiniGameType.BloodCastle, 8),
                new MiniGameReference(MiniGameType.DevilSquare, 7),
                new MiniGameReference(MiniGameType.ChaosCastle, 7),
            ]);
            content.Items.AddRange([
                new ItemRange(12, 36, 40), // third wings
                new ItemRange(12, 43, 43), // Wing of Dimension
                new ItemRange(13, 52, 53), // Flame and Feather of Condor
                new ItemRange(15, 26, 27), // Weakness and Innovation parchments
            ]);
        }),
        Release("swamp-of-calmness", "Swamp of Calmness", ContentStage.Season3, content => content.Maps.Add(56)),

        Release("sistema-de-sockets", "Sistema de sockets", ContentStage.Season4, content =>
        {
            content.Npcs.AddRange([452, 453]); // Seed Master, Seed Researcher
            content.Items.AddRange([
                new ItemRange(0, 26, 28), // Flameberge, Sword Breaker, Imperial Sword
                new ItemRange(2, 16, 17), // Frost Mace, Absolute Scepter
                new ItemRange(4, 23, 23), // Stinger Bow
                new ItemRange(5, 20, 20), // Eternal Wing Stick
                new ItemRange(5, 30, 31), // Deadly Staff, Imperial Staff
                new ItemRange(6, 17, 20), // socket shields
                .. ArmorSets(45, 52),
                new ItemRange(12, 60, 74), // seeds and spheres
                new ItemRange(12, 100, 129), // seed spheres
            ]);
        }),
        Release("armas-y-sets-s4", "Armas y sets de S4", ContentStage.Season4, content =>
        {
            content.Items.AddRange([
                new ItemRange(0, 24, 25), // Daybreak, Sword Dancer
                new ItemRange(0, 31, 31), // Rune Blade
                new ItemRange(2, 15, 15), // Shining Scepter
                new ItemRange(4, 22, 22), // Albatross Bow
                new ItemRange(5, 13, 13), // Platina Staff
                .. ArmorSets(34, 38), // Ashcrow, Eclipse, Iris, Valiant and Glorious
                .. ArmorSets(44, 44), // Lilium
                new ItemRange(12, 44, 48), // crystals and Scroll of Chaotic Diseier
                new ItemRange(15, 28, 29), // Wizardry Enhance and Gigantic Storm scrolls
            ]);
        }),
        Release("raklion", "Raklion", ContentStage.Season4, content =>
        {
            content.Maps.AddRange([57, 58]); // Raklion, Raklion boss
            content.PlugIns.Add(typeof(RaklionPlugIn).GUID);
        }),
        Release("vulcanus", "Vulcanus", ContentStage.Season4, content => content.Maps.Add(63)),
        Release("santa-village", "Santa Village", ContentStage.Season4, content => content.Maps.Add(62)),
        Release("moss-el-mercader", "Moss el mercader", ContentStage.Season4, content => content.Npcs.Add(492)),

        Release("gens", "Gens", ContentStage.Season5, content =>
        {
            content.Npcs.AddRange([543, 544]); // Gens Duprian, Gens Vanert
            content.PlugIns.Add(typeof(GensFeaturePlugIn).GUID);
        }),
        Release("cash-shop", "Cash Shop", ContentStage.Season5, content => content.PlugIns.Add(typeof(CashShopFeaturePlugIn).GUID)),
        Release("duel-arena", "Duel Arena", ContentStage.Season5, content =>
        {
            content.Maps.Add(64);
            content.PlugIns.Add(DuelGroupHandlerPlugInId);
        }),
        Release("doppelganger", "Doppelganger", ContentStage.Season5, content =>
        {
            content.Maps.AddRange([65, 66, 67, 68]);
            content.Npcs.Add(540); // Lugard
            content.MiniGames.Add(new MiniGameReference(MiniGameType.Doppelganger));
            content.Items.AddRange([
                new ItemRange(13, 125, 125), // Doppelganger Free Ticket
                new ItemRange(14, 110, 111), // Sign and Mirror of Dimensions
            ]);
        }),
        Release("imperial-guardian", "Imperial Guardian", ContentStage.Season5, content =>
        {
            content.Maps.AddRange([69, 70, 71, 72]);
            content.Npcs.Add(522); // Adviser Jerinteu
            content.MiniGames.Add(new MiniGameReference(MiniGameType.ImperialGuardian));
            content.Items.Add(new ItemRange(14, 101, 109)); // Suspicious Scrap of Paper, Gaion's Order and Secromicon
        }),
        Release("loren-market", "Loren Market", ContentStage.Season5, content =>
        {
            content.Maps.Add(79);
            content.Npcs.Add(547); // Market Union Member Julia
        }),
        Release("quests-elf-soldier", "Quests del Elf Soldier", ContentStage.Season5, content =>
            content.Quests.AddRange([new QuestReference(15), new QuestReference(18), new QuestReference(19)])),

        Release("rage-fighter", "Rage Fighter", ContentStage.Season6, content =>
        {
            content.CharacterClasses.Add(24);
            content.PlugIns.AddRange([typeof(UnlockRageFighterAtLevel150).GUID, typeof(RageFighterCharacterCardDeliveryPlugIn).GUID]);
            content.Items.AddRange([
                new ItemRange(0, 32, 35), // weapons
                .. ArmorSets(59, 61, withGloves: false),
                .. ArmorSets(73, 73, withGloves: false), // Phoenix Soul
                new ItemRange(12, 49, 50), // capes
                new ItemRange(14, 169, 169), // character card
                new ItemRange(15, 30, 36), // parchments
            ]);
        }),
        Release("karutan", "Karutan", ContentStage.Season6, content => content.Maps.AddRange([80, 81])),
        Release("mercaderes-s6", "Mercaderes de S6", ContentStage.Season6, content => content.Npcs.AddRange([566, 568])), // Felicia, Zyro
    ];

    private static ContentRelease Release(string id, string name, ContentStage era, Action<ReleaseContent> addContent)
    {
        var release = new ContentRelease { Id = id, Name = name, Era = era };
        addContent(release.Content);
        return release;
    }

    private static IEnumerable<ItemRange> ArmorSets(short firstNumber, short lastNumber, bool withGloves = true)
    {
        for (byte group = 7; group <= 11; group++)
        {
            if (group != 10 || withGloves)
            {
                yield return new ItemRange(group, firstNumber, lastNumber);
            }
        }
    }
}
