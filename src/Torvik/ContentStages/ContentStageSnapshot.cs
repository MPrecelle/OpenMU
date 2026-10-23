// <copyright file="ContentStageSnapshot.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// The original values of the configuration data which the <see cref="ContentStageApplier"/> changes to lock content.
/// They are restored when the content gets unlocked again.
/// </summary>
public class ContentStageSnapshot
{
    /// <summary>
    /// Gets or sets the original values of the items, by "group:number".
    /// </summary>
    public Dictionary<string, ItemState> Items { get; set; } = new();

    /// <summary>
    /// Gets or sets the original <see cref="CharacterClass.CanGetCreated"/>, by class number.
    /// </summary>
    public Dictionary<byte, bool> CharacterClasses { get; set; } = new();

    /// <summary>
    /// Gets or sets the original minimum character levels of the quests, by "giver:group:number:class".
    /// </summary>
    public Dictionary<string, int> Quests { get; set; } = new();

    /// <summary>
    /// Gets or sets the original minimum character levels of the mini games, by "type:level".
    /// </summary>
    public Dictionary<string, MiniGameState> MiniGames { get; set; } = new();

    /// <summary>
    /// Gets or sets the original quantities of the spawns of NPCs, by spawn id.
    /// </summary>
    public Dictionary<Guid, short> Spawns { get; set; } = new();

    /// <summary>
    /// Gets or sets the original activation state of the plugins, by plugin type id.
    /// </summary>
    public Dictionary<Guid, bool> PlugIns { get; set; } = new();

    /// <summary>
    /// The original values of an item definition.
    /// </summary>
    /// <param name="DropsFromMonsters">The original <see cref="ItemDefinition.DropsFromMonsters"/>.</param>
    /// <param name="MaximumSockets">The original <see cref="ItemDefinition.MaximumSockets"/>.</param>
    public record ItemState(bool DropsFromMonsters, int MaximumSockets);

    /// <summary>
    /// The original values of a mini game definition.
    /// </summary>
    /// <param name="MinimumCharacterLevel">The original <see cref="MiniGameDefinition.MinimumCharacterLevel"/>.</param>
    /// <param name="MinimumSpecialCharacterLevel">The original <see cref="MiniGameDefinition.MinimumSpecialCharacterLevel"/>.</param>
    public record MiniGameState(int MinimumCharacterLevel, int MinimumSpecialCharacterLevel);
}
