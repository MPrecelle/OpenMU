// <copyright file="ContentLocks.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

using MUnique.OpenMU.DataModel.Configuration.Quests;

/// <summary>
/// The content which is locked because its <see cref="ContentRelease"/> isn't released yet.
/// It tells for each part of the content which release locks it.
/// </summary>
public sealed class ContentLocks
{
    private readonly Dictionary<short, ContentRelease> _maps = new();
    private readonly Dictionary<short, ContentRelease> _npcs = new();
    private readonly Dictionary<byte, ContentRelease> _characterClasses = new();
    private readonly Dictionary<Guid, ContentRelease> _plugIns = new();
    private readonly List<(ReleaseContent.ItemRange Range, ContentRelease Release)> _items = new();
    private readonly List<(ReleaseContent.QuestReference Quest, ContentRelease Release)> _quests = new();
    private readonly List<(ReleaseContent.MiniGameReference MiniGame, ContentRelease Release)> _miniGames = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentLocks"/> class.
    /// </summary>
    /// <param name="lockedReleases">The releases which aren't released yet.</param>
    public ContentLocks(IEnumerable<ContentRelease> lockedReleases)
    {
        foreach (var release in lockedReleases)
        {
            var content = release.Content;
            content.Maps.ForEach(map => this._maps.TryAdd(map, release));
            content.Npcs.ForEach(npc => this._npcs.TryAdd(npc, release));
            content.CharacterClasses.ForEach(characterClass => this._characterClasses.TryAdd(characterClass, release));
            content.PlugIns.ForEach(plugIn => this._plugIns.TryAdd(plugIn, release));
            this._items.AddRange(content.Items.Select(range => (range, release)));
            this._quests.AddRange(content.Quests.Select(quest => (quest, release)));
            this._miniGames.AddRange(content.MiniGames.Select(miniGame => (miniGame, release)));
        }
    }

    /// <summary>
    /// Gets the locks of a configuration in which everything is released.
    /// </summary>
    public static ContentLocks None { get; } = new([]);

    /// <summary>
    /// Gets the release which locks the map.
    /// </summary>
    /// <param name="map">The map.</param>
    /// <returns>The release; <c>null</c>, if the map isn't locked.</returns>
    public ContentRelease? GetRelease(GameMapDefinition map) => this._maps.GetValueOrDefault(map.Number);

    /// <summary>
    /// Gets the release which locks the item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The release; <c>null</c>, if the item isn't locked.</returns>
    public ContentRelease? GetRelease(ItemDefinition item) => this._items.FirstOrDefault(entry => entry.Range.Contains(item)).Release;

    /// <summary>
    /// Gets the release which locks the spawns of the NPC or monster.
    /// </summary>
    /// <param name="monster">The NPC or monster.</param>
    /// <returns>The release; <c>null</c>, if the NPC isn't locked.</returns>
    public ContentRelease? GetRelease(MonsterDefinition monster) => this._npcs.GetValueOrDefault(monster.Number);

    /// <summary>
    /// Gets the release which locks the creation of characters of the class.
    /// </summary>
    /// <param name="characterClass">The character class.</param>
    /// <returns>The release; <c>null</c>, if the class isn't locked.</returns>
    public ContentRelease? GetRelease(CharacterClass characterClass) => this._characterClasses.GetValueOrDefault(characterClass.Number);

    /// <summary>
    /// Gets the release which locks the quest.
    /// </summary>
    /// <param name="quest">The quest.</param>
    /// <returns>The release; <c>null</c>, if the quest isn't locked.</returns>
    public ContentRelease? GetRelease(QuestDefinition quest) => this._quests.FirstOrDefault(entry => entry.Quest.Matches(quest)).Release;

    /// <summary>
    /// Gets the release which locks the mini game.
    /// </summary>
    /// <param name="miniGame">The mini game.</param>
    /// <returns>The release; <c>null</c>, if the mini game isn't locked.</returns>
    public ContentRelease? GetRelease(MiniGameDefinition miniGame) => this._miniGames.FirstOrDefault(entry => entry.MiniGame.Matches(miniGame)).Release;

    /// <summary>
    /// Gets the release which keeps the plugin deactivated.
    /// </summary>
    /// <param name="plugInTypeId">The type id of the plugin.</param>
    /// <returns>The release; <c>null</c>, if the plugin isn't locked.</returns>
    public ContentRelease? GetPlugInRelease(Guid plugInTypeId) => this._plugIns.GetValueOrDefault(plugInTypeId);
}
