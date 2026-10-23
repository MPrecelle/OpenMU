// <copyright file="ContentStageApplier.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Brings the configuration data to a <see cref="ContentStage"/>: the content of later stages
/// gets locked, the other content gets its original values back.
/// </summary>
/// <remarks>
/// Applying a stage is idempotent. The original values are kept in the <see cref="ContentStageSnapshot"/> of the
/// <see cref="ContentStageConfiguration"/>, which is removed again when <see cref="ContentStage.Season6"/> is applied.
/// Changes of the game servers' data (e.g. which items drop) take effect after their configuration was reloaded.
/// </remarks>
public sealed class ContentStageApplier
{
    /// <summary>
    /// The minimum character level of quests and mini games which are locked. No character reaches it.
    /// </summary>
    public const int LockedLevel = short.MaxValue;

    private readonly IContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentStageApplier"/> class.
    /// </summary>
    /// <param name="context">The context in which the game configuration was loaded. It's used to create the missing plugin configuration.</param>
    public ContentStageApplier(IContext context)
    {
        this._context = context;
    }

    /// <summary>
    /// Gets the configuration of the <see cref="ContentStagesPlugIn"/>, as stored in the game configuration.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <returns>The configuration; the default one, if none is stored yet.</returns>
    public static ContentStageConfiguration GetConfiguration(GameConfiguration gameConfiguration)
    {
        var plugInConfiguration = gameConfiguration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == ContentStagesPlugIn.TypeId);
        return plugInConfiguration?.GetConfiguration<ContentStageConfiguration>(null) ?? new ContentStageConfiguration();
    }

    /// <summary>
    /// Applies the stage to the game configuration. The changes still need to be saved.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="stage">The stage.</param>
    /// <returns>The resulting configuration of the <see cref="ContentStagesPlugIn"/>.</returns>
    public ContentStageConfiguration Apply(GameConfiguration gameConfiguration, ContentStage stage)
    {
        var plugInConfiguration = this.GetOrCreatePlugInConfiguration(gameConfiguration);
        var configuration = plugInConfiguration.GetConfiguration<ContentStageConfiguration>(null) ?? new ContentStageConfiguration();
        var snapshot = configuration.Snapshot ?? new ContentStageSnapshot();

        ApplyToItems(gameConfiguration, stage, snapshot);
        ApplyToCharacterClasses(gameConfiguration, stage, snapshot);
        ApplyToQuests(gameConfiguration, stage, snapshot);
        ApplyToMiniGames(gameConfiguration, stage, snapshot);
        ApplyToSpawns(gameConfiguration, stage, snapshot);
        ApplyToPlugIns(gameConfiguration, stage, snapshot);

        configuration.CurrentStage = stage;

        // When everything is unlocked, all original values are back and the snapshot isn't needed anymore.
        configuration.Snapshot = stage == ContentStage.Season6 ? null : snapshot;
        plugInConfiguration.SetConfiguration(configuration, null);
        return configuration;
    }

    private static bool IsLocked(ContentStage contentStage, ContentStage currentStage) => contentStage > currentStage;

    private static void ApplyToItems(GameConfiguration gameConfiguration, ContentStage stage, ContentStageSnapshot snapshot)
    {
        foreach (var item in gameConfiguration.Items)
        {
            var itemStage = ContentCatalog.GetStage(item);
            if (itemStage == ContentStage.Classic)
            {
                continue;
            }

            var key = $"{item.Group}:{item.Number}";
            if (IsLocked(itemStage, stage))
            {
                snapshot.Items.TryAdd(key, new ContentStageSnapshot.ItemState(item.DropsFromMonsters, item.MaximumSockets));
                item.DropsFromMonsters = false;
                item.MaximumSockets = 0;
            }
            else if (snapshot.Items.Remove(key, out var original))
            {
                item.DropsFromMonsters = original.DropsFromMonsters;
                item.MaximumSockets = original.MaximumSockets;
            }
        }
    }

    private static void ApplyToCharacterClasses(GameConfiguration gameConfiguration, ContentStage stage, ContentStageSnapshot snapshot)
    {
        foreach (var characterClass in gameConfiguration.CharacterClasses)
        {
            if (!ContentCatalog.CharacterClasses.TryGetValue(characterClass.Number, out var classStage))
            {
                continue;
            }

            if (IsLocked(classStage, stage))
            {
                snapshot.CharacterClasses.TryAdd(characterClass.Number, characterClass.CanGetCreated);
                characterClass.CanGetCreated = false;
            }
            else if (snapshot.CharacterClasses.Remove(characterClass.Number, out var original))
            {
                characterClass.CanGetCreated = original;
            }
        }
    }

    private static void ApplyToQuests(GameConfiguration gameConfiguration, ContentStage stage, ContentStageSnapshot snapshot)
    {
        foreach (var monster in gameConfiguration.Monsters)
        {
            foreach (var quest in monster.Quests)
            {
                var questStage = ContentCatalog.GetStage(quest);
                if (questStage == ContentStage.Classic)
                {
                    continue;
                }

                var key = $"{monster.Number}:{quest.Group}:{quest.Number}:{quest.QualifiedCharacter?.Number}";
                if (IsLocked(questStage, stage))
                {
                    snapshot.Quests.TryAdd(key, quest.MinimumCharacterLevel);
                    quest.MinimumCharacterLevel = LockedLevel;
                }
                else if (snapshot.Quests.Remove(key, out var original))
                {
                    quest.MinimumCharacterLevel = original;
                }
            }
        }
    }

    private static void ApplyToMiniGames(GameConfiguration gameConfiguration, ContentStage stage, ContentStageSnapshot snapshot)
    {
        foreach (var miniGame in gameConfiguration.MiniGameDefinitions)
        {
            var miniGameStage = ContentCatalog.GetStage(miniGame);
            if (miniGameStage == ContentStage.Classic)
            {
                continue;
            }

            var key = $"{miniGame.Type}:{miniGame.GameLevel}";
            if (IsLocked(miniGameStage, stage))
            {
                snapshot.MiniGames.TryAdd(key, new ContentStageSnapshot.MiniGameState(miniGame.MinimumCharacterLevel, miniGame.MinimumSpecialCharacterLevel));
                miniGame.MinimumCharacterLevel = LockedLevel;
                miniGame.MinimumSpecialCharacterLevel = LockedLevel;
            }
            else if (snapshot.MiniGames.Remove(key, out var original))
            {
                miniGame.MinimumCharacterLevel = original.MinimumCharacterLevel;
                miniGame.MinimumSpecialCharacterLevel = original.MinimumSpecialCharacterLevel;
            }
        }
    }

    private static void ApplyToSpawns(GameConfiguration gameConfiguration, ContentStage stage, ContentStageSnapshot snapshot)
    {
        foreach (var spawn in gameConfiguration.Maps.SelectMany(m => m.MonsterSpawns))
        {
            var spawnStage = spawn.MonsterDefinition is { } monster ? ContentCatalog.GetStage(monster) : ContentStage.Classic;
            if (spawnStage == ContentStage.Classic)
            {
                continue;
            }

            var key = spawn.GetId();
            if (IsLocked(spawnStage, stage))
            {
                snapshot.Spawns.TryAdd(key, spawn.Quantity);
                spawn.Quantity = 0;
            }
            else if (snapshot.Spawns.Remove(key, out var original))
            {
                spawn.Quantity = original;
            }
        }
    }

    private static void ApplyToPlugIns(GameConfiguration gameConfiguration, ContentStage stage, ContentStageSnapshot snapshot)
    {
        foreach (var plugInConfiguration in gameConfiguration.PlugInConfigurations)
        {
            if (!ContentCatalog.PlugIns.TryGetValue(plugInConfiguration.TypeId, out var plugInStage))
            {
                continue;
            }

            if (IsLocked(plugInStage, stage))
            {
                snapshot.PlugIns.TryAdd(plugInConfiguration.TypeId, plugInConfiguration.IsActive);
                plugInConfiguration.IsActive = false;
            }
            else if (snapshot.PlugIns.Remove(plugInConfiguration.TypeId, out var original))
            {
                plugInConfiguration.IsActive = original;
            }
        }
    }

    private PlugInConfiguration GetOrCreatePlugInConfiguration(GameConfiguration gameConfiguration)
    {
        if (gameConfiguration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == ContentStagesPlugIn.TypeId) is { } existing)
        {
            return existing;
        }

        var plugInConfiguration = this._context.CreateNew<PlugInConfiguration>();
        plugInConfiguration.TypeId = ContentStagesPlugIn.TypeId;
        plugInConfiguration.IsActive = true;
        gameConfiguration.PlugInConfigurations.Add(plugInConfiguration);
        return plugInConfiguration;
    }
}
