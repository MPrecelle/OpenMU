// <copyright file="DropLogPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Torvik.Properties;

/// <summary>
/// Logs the items which killed monsters drop, to verify the locks of the <see cref="ContentStagesPlugIn"/> in the game.
/// The entries start with <see cref="LogPrefix"/>.
/// </summary>
[PlugIn]
[Display(Name = nameof(ContentStageResources.DropLogPlugIn_Name), Description = nameof(ContentStageResources.DropLogPlugIn_Description), ResourceType = typeof(ContentStageResources))]
[Guid("6B1E2C7D-3F0A-4E8B-9C51-2D7A4F6E8B13")]
public sealed class DropLogPlugIn : IMonsterItemDroppedPlugIn
{
    /// <summary>
    /// The prefix of the log entries of the drops, and of the drops canceled by the <see cref="ContentStagesPlugIn"/>.
    /// </summary>
    public const string LogPrefix = "Drop";

    /// <inheritdoc />
    public ValueTask MonsterItemDroppedAsync(AttackableNpcBase monster, Player killer, DroppedItem droppedItem)
    {
        var item = droppedItem.Item;
        killer.Logger.LogInformation(
            LogPrefix + ": {Item} ({Group}:{Number}) from {Monster} on {Map}, killed by {Player}",
            item.Definition?.GetNameForLevel(item.Level),
            item.Definition?.Group,
            item.Definition?.Number,
            monster.Definition.Designation.ValueInNeutralLanguage,
            monster.CurrentMap.Definition.Name.ValueInNeutralLanguage,
            killer.Name);
        return ValueTask.CompletedTask;
    }
}
