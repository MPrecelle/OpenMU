// <copyright file="ContentStagesPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Torvik.Properties;

/// <summary>
/// Locks the content of the <see cref="ContentStageConfiguration.Releases"/> which aren't released yet while the game runs:
/// the maps can't be entered and the items don't drop from monsters.
/// When a player enters the world, it shows the locked content to the game client (<see cref="IShowLockedContentPlugIn"/>).
/// The configuration data is changed by the <see cref="ContentStageApplier"/>.
/// </summary>
[PlugIn]
[Display(Name = nameof(ContentStageResources.PlugIn_Name), Description = nameof(ContentStageResources.PlugIn_Description), ResourceType = typeof(ContentStageResources))]
[Guid(TypeIdString)]
public sealed class ContentStagesPlugIn : IFeaturePlugIn, IWarpGateEnteringPlugIn, IPlayerStateChangedPlugIn, IMonsterItemDropPlugIn, ISupportCustomConfiguration<ContentStageConfiguration>, ISupportDefaultCustomConfiguration
{
    private const string TypeIdString = "9FA6C153-9399-4C59-8896-79CE758AD270";

    private ContentStageConfiguration? _configuration;

    private ContentLocks _locks = ContentLocks.None;

    /// <summary>
    /// Gets the type id of this plugin.
    /// </summary>
    public static Guid TypeId { get; } = new(TypeIdString);

    /// <inheritdoc />
    public ContentStageConfiguration? Configuration
    {
        get => this._configuration;
        set
        {
            this._configuration = value;
            this._locks = value?.CreateLocks() ?? ContentLocks.None;
        }
    }

    /// <inheritdoc />
    public async ValueTask WarpGateEnteringAsync(Player player, ExitGate targetGate, CancelEventArgs eventArgs)
    {
        if (targetGate.Map is { } map && this._locks.GetRelease(map) is not null)
        {
            eventArgs.Cancel = true;
            var message = ContentStageResources.ResourceManager.GetString(nameof(ContentStageResources.MapLocked), player.Culture) ?? string.Empty;
            await player.ShowBlueMessageAsync(message).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (currentState == PlayerState.EnteredWorld)
        {
            var content = this._locks.ClientContent;
            await player.InvokeViewPlugInAsync<IShowLockedContentPlugIn>(p => p.ShowLockedContentAsync(content)).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void ItemDropping(AttackableNpcBase monster, Player killer, Item item, CancelEventArgs eventArgs)
    {
        // Covers the drop groups with explicit items, which don't check ItemDefinition.DropsFromMonsters.
        if (item.Definition is { } definition && this._locks.GetRelease(definition) is { } release)
        {
            eventArgs.Cancel = true;
            killer.Logger.LogInformation(
                DropLogPlugIn.LogPrefix + " blocked by the release {Release}: {Item} ({Group}:{Number}) from {Monster}, killed by {Player}",
                release.Id,
                definition.GetNameForLevel(item.Level),
                definition.Group,
                definition.Number,
                monster.Definition.Designation.ValueInNeutralLanguage,
                killer.Name);
        }
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new ContentStageConfiguration();
}
