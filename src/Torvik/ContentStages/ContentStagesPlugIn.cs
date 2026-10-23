// <copyright file="ContentStagesPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

using System.ComponentModel;
using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Torvik.Properties;

/// <summary>
/// Locks the content of the stages after the <see cref="ContentStageConfiguration.CurrentStage"/> while the game runs:
/// the maps can't be entered and the items don't drop from monsters.
/// The configuration data is changed by the <see cref="ContentStageApplier"/>.
/// </summary>
[PlugIn]
[Display(Name = nameof(ContentStageResources.PlugIn_Name), Description = nameof(ContentStageResources.PlugIn_Description), ResourceType = typeof(ContentStageResources))]
[Guid(TypeIdString)]
public sealed class ContentStagesPlugIn : IFeaturePlugIn, IWarpGateEnteringPlugIn, IMonsterItemDropPlugIn, ISupportCustomConfiguration<ContentStageConfiguration>, ISupportDefaultCustomConfiguration
{
    private const string TypeIdString = "9FA6C153-9399-4C59-8896-79CE758AD270";

    /// <summary>
    /// Gets the type id of this plugin.
    /// </summary>
    public static Guid TypeId { get; } = new(TypeIdString);

    /// <inheritdoc />
    public ContentStageConfiguration? Configuration { get; set; }

    private ContentStage CurrentStage => this.Configuration?.CurrentStage ?? ContentStage.Season6;

    /// <inheritdoc />
    public async ValueTask WarpGateEnteringAsync(Player player, ExitGate targetGate, CancelEventArgs eventArgs)
    {
        if (targetGate.Map is { } map && ContentCatalog.GetStage(map) > this.CurrentStage)
        {
            eventArgs.Cancel = true;
            var message = ContentStageResources.ResourceManager.GetString(nameof(ContentStageResources.MapLocked), player.Culture) ?? string.Empty;
            await player.ShowBlueMessageAsync(message).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void ItemDropping(AttackableNpcBase monster, Player killer, Item item, CancelEventArgs eventArgs)
    {
        // Covers the drop groups with explicit items, which don't check ItemDefinition.DropsFromMonsters.
        if (item.Definition is { } definition && ContentCatalog.GetStage(definition) > this.CurrentStage)
        {
            eventArgs.Cancel = true;
        }
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new ContentStageConfiguration();
}
