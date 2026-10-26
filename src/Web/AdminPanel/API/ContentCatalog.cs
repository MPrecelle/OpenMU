// <copyright file="ContentCatalog.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Text.Json.Serialization;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// Torvik MU: the content of the game configuration which can be part of a <see cref="ContentRelease"/>, with its names,
/// for the editor of the releases.
/// </summary>
/// <param name="Maps">The maps.</param>
/// <param name="Items">The items.</param>
/// <param name="Npcs">The NPCs.</param>
/// <param name="CharacterClasses">The character classes.</param>
/// <param name="Quests">The quests, one per group and number.</param>
/// <param name="MiniGames">The mini games, one per type and level.</param>
/// <param name="PlugIns">The plugins.</param>
public sealed record ContentCatalog(
    IReadOnlyList<ContentCatalog.Map> Maps,
    IReadOnlyList<ContentCatalog.Item> Items,
    IReadOnlyList<ContentCatalog.Npc> Npcs,
    IReadOnlyList<ContentCatalog.CharacterClass> CharacterClasses,
    IReadOnlyList<ContentCatalog.Quest> Quests,
    IReadOnlyList<ContentCatalog.MiniGame> MiniGames,
    IReadOnlyList<ContentCatalog.PlugIn> PlugIns)
{
    /// <summary>
    /// A map.
    /// </summary>
    /// <param name="Number">The number.</param>
    /// <param name="Name">The name.</param>
    public sealed record Map(short Number, string Name);

    /// <summary>
    /// An item.
    /// </summary>
    /// <param name="Group">The group.</param>
    /// <param name="Number">The number in the group.</param>
    /// <param name="Name">The name.</param>
    public sealed record Item(byte Group, short Number, string Name);

    /// <summary>
    /// An NPC.
    /// </summary>
    /// <param name="Number">The number of its monster definition.</param>
    /// <param name="Name">The name.</param>
    public sealed record Npc(short Number, string Name);

    /// <summary>
    /// A character class.
    /// </summary>
    /// <param name="Number">The number.</param>
    /// <param name="Name">The name.</param>
    public sealed record CharacterClass(byte Number, string Name);

    /// <summary>
    /// A quest.
    /// </summary>
    /// <param name="Group">The group.</param>
    /// <param name="Number">The number in the group.</param>
    /// <param name="Name">The name.</param>
    public sealed record Quest(short Group, short Number, string Name);

    /// <summary>
    /// A mini game.
    /// </summary>
    /// <param name="Type">The type.</param>
    /// <param name="GameLevel">The level.</param>
    /// <param name="Name">The name.</param>
    public sealed record MiniGame([property: JsonConverter(typeof(JsonStringEnumConverter))] MiniGameType Type, byte GameLevel, string Name);

    /// <summary>
    /// A plugin.
    /// </summary>
    /// <param name="Id">The id of its type.</param>
    /// <param name="Name">The name.</param>
    /// <param name="Description">The description.</param>
    public sealed record PlugIn(Guid Id, string Name, string? Description);
}
