// <copyright file="ContentCatalog.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.Gens;
using MUnique.OpenMU.GameLogic.PlugIns.UnlockCharacterClass;
using MUnique.OpenMU.GameLogic.Raklion;

/// <summary>
/// The catalog of the content which isn't available from the start, with the stage which unlocks it.
/// Everything which isn't listed here is <see cref="ContentStage.Classic"/>.
/// </summary>
/// <remarks>
/// The numbers are the ones of the season 6 data initialization. See notes/classic-mode.md of the workspace.
/// </remarks>
public static class ContentCatalog
{
    /// <summary>
    /// The type id of the internal <c>DuelGroupHandlerPlugIn</c> of the game server, which handles the duel requests.
    /// </summary>
    private static readonly Guid DuelGroupHandlerPlugInId = new("88A8E1DB-C3EC-45A0-98F9-E9DA6F17373C");

    /// <summary>
    /// Gets the maps by their number.
    /// </summary>
    public static IReadOnlyDictionary<short, ContentStage> Maps { get; } = new Dictionary<short, ContentStage>
    {
        [41] = ContentStage.Season3, // Barracks of Balgass
        [42] = ContentStage.Season3, // Balgass Refuge
        [56] = ContentStage.Season3, // Swamp of Calmness
        [57] = ContentStage.Season4, // Raklion
        [58] = ContentStage.Season4, // Raklion boss
        [62] = ContentStage.Season4, // Santa Village
        [63] = ContentStage.Season4, // Vulcanus
        [64] = ContentStage.Season5, // Duel Arena
        [65] = ContentStage.Season5, // Doppelganger 1-4
        [66] = ContentStage.Season5,
        [67] = ContentStage.Season5,
        [68] = ContentStage.Season5,
        [69] = ContentStage.Season5, // Imperial Guardian 1-4
        [70] = ContentStage.Season5,
        [71] = ContentStage.Season5,
        [72] = ContentStage.Season5,
        [79] = ContentStage.Season5, // Loren Market
        [80] = ContentStage.Season6, // Karutan 1-2
        [81] = ContentStage.Season6,
    };

    /// <summary>
    /// Gets the character classes which can be created, by their number.
    /// </summary>
    public static IReadOnlyDictionary<byte, ContentStage> CharacterClasses { get; } = new Dictionary<byte, ContentStage>
    {
        [24] = ContentStage.Season6, // Rage Fighter
    };

    /// <summary>
    /// Gets the quests of the legacy quest group (0), by their number.
    /// </summary>
    public static IReadOnlyDictionary<short, ContentStage> LegacyQuests { get; } = new Dictionary<short, ContentStage>
    {
        [4] = ContentStage.Season3, // third evolution, quests of Apostle Devin
        [5] = ContentStage.Season3,
        [6] = ContentStage.Season3,
    };

    /// <summary>
    /// Gets the plugins by their type id.
    /// </summary>
    public static IReadOnlyDictionary<Guid, ContentStage> PlugIns { get; } = new Dictionary<Guid, ContentStage>
    {
        [typeof(RaklionPlugIn).GUID] = ContentStage.Season4,
        [typeof(GensFeaturePlugIn).GUID] = ContentStage.Season5,
        [typeof(CashShopFeaturePlugIn).GUID] = ContentStage.Season5,
        [DuelGroupHandlerPlugInId] = ContentStage.Season5,
        [typeof(UnlockRageFighterAtLevel150).GUID] = ContentStage.Season6,
        [typeof(RageFighterCharacterCardDeliveryPlugIn).GUID] = ContentStage.Season6,
    };

    /// <summary>
    /// Gets the items, as ranges of numbers within a group.
    /// </summary>
    public static IReadOnlyList<ItemRange> Items { get; } =
    [
        new(12, 36, 40, ContentStage.Season3), // third wings
        new(12, 43, 43, ContentStage.Season3), // Wing of Dimension
        new(13, 52, 53, ContentStage.Season3), // Flame and Feather of Condor
        new(15, 26, 27, ContentStage.Season3), // Weakness and Innovation parchments

        new(0, 24, 28, ContentStage.Season4), // Daybreak, Sword Dancer and socket swords
        new(0, 31, 31, ContentStage.Season4), // Rune Blade
        new(2, 15, 17, ContentStage.Season4), // Shining Scepter and socket maces
        new(4, 22, 23, ContentStage.Season4), // Albatross Bow and Stinger Bow
        new(5, 13, 13, ContentStage.Season4), // Platina Staff
        new(5, 20, 20, ContentStage.Season4), // Eternal Wing Stick
        new(5, 30, 31, ContentStage.Season4), // socket staffs
        new(6, 17, 20, ContentStage.Season4), // socket shields
        .. ArmorSets(34, 38, ContentStage.Season4), // Ashcrow, Eclipse, Iris, Valiant and Glorious
        .. ArmorSets(44, 52, ContentStage.Season4), // Lilium and the socket sets
        new(12, 44, 48, ContentStage.Season4), // crystals and Scroll of Chaotic Diseier
        new(12, 60, 74, ContentStage.Season4), // seeds and spheres
        new(12, 100, 129, ContentStage.Season4), // seed spheres
        new(15, 28, 29, ContentStage.Season4), // Wizardry Enhance and Gigantic Storm scrolls

        new(13, 125, 125, ContentStage.Season5), // Doppelganger Free Ticket
        new(14, 101, 111, ContentStage.Season5), // Imperial Guardian and Doppelganger items

        new(0, 32, 35, ContentStage.Season6), // Rage Fighter weapons
        .. ArmorSets(59, 61, ContentStage.Season6, withGloves: false), // Rage Fighter sets
        .. ArmorSets(73, 73, ContentStage.Season6, withGloves: false), // Phoenix Soul
        new(12, 49, 50, ContentStage.Season6), // Rage Fighter capes
        new(14, 169, 169, ContentStage.Season6), // Rage Fighter character card
        new(15, 30, 36, ContentStage.Season6), // Rage Fighter parchments
    ];

    /// <summary>
    /// Gets the stage which unlocks the map.
    /// </summary>
    /// <param name="map">The map.</param>
    /// <returns>The stage which unlocks the map.</returns>
    public static ContentStage GetStage(GameMapDefinition map) => Maps.GetValueOrDefault(map.Number);

    /// <summary>
    /// Gets the stage which unlocks the item.
    /// </summary>
    /// <param name="item">The item definition.</param>
    /// <returns>The stage which unlocks the item.</returns>
    public static ContentStage GetStage(ItemDefinition item)
    {
        foreach (var range in Items)
        {
            if (range.Contains(item))
            {
                return range.Stage;
            }
        }

        return ContentStage.Classic;
    }

    /// <summary>
    /// Gets the stage which unlocks the mini game.
    /// </summary>
    /// <param name="miniGame">The mini game definition.</param>
    /// <returns>The stage which unlocks the mini game.</returns>
    public static ContentStage GetStage(MiniGameDefinition miniGame) => miniGame switch
    {
        { Type: MiniGameType.Doppelganger or MiniGameType.ImperialGuardian } => ContentStage.Season5,
        { RequiresMasterClass: true } => ContentStage.Season3, // Blood Castle 8, Devil Square 7, Chaos Castle 7
        _ => ContentStage.Classic,
    };

    /// <summary>
    /// Gets the stage which unlocks the quest.
    /// </summary>
    /// <param name="quest">The quest definition.</param>
    /// <returns>The stage which unlocks the quest.</returns>
    public static ContentStage GetStage(DataModel.Configuration.Quests.QuestDefinition quest)
        => quest.Group == 0 ? LegacyQuests.GetValueOrDefault(quest.Number) : ContentStage.Classic;

    /// <summary>
    /// Creates the ranges of the armor pieces (groups 7 to 11: helm, armor, pants, gloves and boots) of the sets.
    /// The sets of the Rage Fighter have no gloves.
    /// </summary>
    private static IEnumerable<ItemRange> ArmorSets(short firstNumber, short lastNumber, ContentStage stage, bool withGloves = true)
    {
        for (byte group = 7; group <= 11; group++)
        {
            if (group != 10 || withGloves)
            {
                yield return new ItemRange(group, firstNumber, lastNumber, stage);
            }
        }
    }

    /// <summary>
    /// A range of item numbers within an item group.
    /// </summary>
    /// <param name="Group">The item group.</param>
    /// <param name="FirstNumber">The first item number.</param>
    /// <param name="LastNumber">The last item number, inclusive.</param>
    /// <param name="Stage">The stage which unlocks the items.</param>
    public record ItemRange(byte Group, short FirstNumber, short LastNumber, ContentStage Stage)
    {
        /// <summary>
        /// Determines whether the item belongs to this range.
        /// </summary>
        /// <param name="item">The item definition.</param>
        /// <returns><c>true</c>, if the item belongs to this range.</returns>
        public bool Contains(ItemDefinition item)
            => item.Group == this.Group && item.Number >= this.FirstNumber && item.Number <= this.LastNumber;
    }
}
