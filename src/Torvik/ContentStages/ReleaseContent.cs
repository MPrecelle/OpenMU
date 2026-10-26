// <copyright file="ReleaseContent.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

using System.Text.Json.Serialization;
using MUnique.OpenMU.DataModel.Configuration.Quests;

/// <summary>
/// The content which a <see cref="ContentRelease"/> unlocks. Until the release, it's locked.
/// </summary>
public class ReleaseContent
{
    /// <summary>
    /// Gets or sets the numbers of the maps, which can't be entered while locked.
    /// </summary>
    public List<short> Maps { get; set; } = new();

    /// <summary>
    /// Gets or sets the ranges of the items, which don't drop and have no sockets while locked.
    /// </summary>
    public List<ItemRange> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the numbers of the NPCs and monsters, which aren't spawned while locked.
    /// </summary>
    public List<short> Npcs { get; set; } = new();

    /// <summary>
    /// Gets or sets the numbers of the character classes, which can't be created while locked.
    /// </summary>
    public List<byte> CharacterClasses { get; set; } = new();

    /// <summary>
    /// Gets or sets the quests, which no character can start while locked.
    /// </summary>
    public List<QuestReference> Quests { get; set; } = new();

    /// <summary>
    /// Gets or sets the mini games, which no character can enter while locked.
    /// </summary>
    public List<MiniGameReference> MiniGames { get; set; } = new();

    /// <summary>
    /// Gets or sets the type ids of the plugins, which are deactivated while locked.
    /// </summary>
    public List<Guid> PlugIns { get; set; } = new();

    /// <summary>
    /// A range of item numbers of an item group.
    /// </summary>
    /// <param name="Group">The item group.</param>
    /// <param name="FirstNumber">The first item number.</param>
    /// <param name="LastNumber">The last item number, inclusive.</param>
    public record ItemRange(byte Group, short FirstNumber, short LastNumber)
    {
        /// <summary>
        /// Determines whether the range contains the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c>, if the range contains the item.</returns>
        public bool Contains(ItemDefinition item)
            => item.Group == this.Group && item.Number >= this.FirstNumber && item.Number <= this.LastNumber;

        /// <summary>
        /// Determines whether the range shares items with another range.
        /// </summary>
        /// <param name="other">The other range.</param>
        /// <returns><c>true</c>, if both ranges contain an item number of the same group.</returns>
        public bool Overlaps(ItemRange other)
            => other.Group == this.Group && other.FirstNumber <= this.LastNumber && this.FirstNumber <= other.LastNumber;

        /// <inheritdoc />
        public override string ToString() => $"items {this.Group}:{this.FirstNumber}-{this.LastNumber}";
    }

    /// <summary>
    /// A quest, or all quests of a quest group.
    /// </summary>
    /// <param name="Group">The quest group.</param>
    /// <param name="Number">The quest number; <c>null</c> for all quests of the group.</param>
    public record QuestReference(short Group, short? Number = null)
    {
        /// <summary>
        /// Determines whether the reference matches the quest.
        /// </summary>
        /// <param name="quest">The quest.</param>
        /// <returns><c>true</c>, if the reference matches the quest.</returns>
        public bool Matches(QuestDefinition quest)
            => quest.Group == this.Group && (this.Number is null || quest.Number == this.Number);

        /// <summary>
        /// Determines whether the reference shares quests with another reference.
        /// </summary>
        /// <param name="other">The other reference.</param>
        /// <returns><c>true</c>, if both references match a quest.</returns>
        public bool Overlaps(QuestReference other)
            => other.Group == this.Group && (this.Number is null || other.Number is null || other.Number == this.Number);

        /// <inheritdoc />
        public override string ToString() => this.Number is { } number ? $"quest {this.Group}:{number}" : $"quests {this.Group}";
    }

    /// <summary>
    /// A level of a mini game, or all of its levels.
    /// </summary>
    /// <param name="Type">The type of the mini game.</param>
    /// <param name="GameLevel">The level; <c>null</c> for all levels.</param>
    public record MiniGameReference([property: JsonConverter(typeof(JsonStringEnumConverter))] MiniGameType Type, byte? GameLevel = null)
    {
        /// <summary>
        /// Determines whether the reference matches the mini game.
        /// </summary>
        /// <param name="miniGame">The mini game.</param>
        /// <returns><c>true</c>, if the reference matches the mini game.</returns>
        public bool Matches(MiniGameDefinition miniGame)
            => miniGame.Type == this.Type && (this.GameLevel is null || miniGame.GameLevel == this.GameLevel);

        /// <summary>
        /// Determines whether the reference shares mini games with another reference.
        /// </summary>
        /// <param name="other">The other reference.</param>
        /// <returns><c>true</c>, if both references match a mini game.</returns>
        public bool Overlaps(MiniGameReference other)
            => other.Type == this.Type && (this.GameLevel is null || other.GameLevel is null || other.GameLevel == this.GameLevel);

        /// <inheritdoc />
        public override string ToString() => this.GameLevel is { } level ? $"mini game {this.Type} {level}" : $"mini game {this.Type}";
    }
}
