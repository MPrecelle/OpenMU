// <copyright file="ContentStageTestData.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// Creates the season 6 data in memory, as the server has it after its installation.
/// </summary>
internal static class ContentStageTestData
{
    /// <summary>
    /// Creates the season 6 data and loads its game configuration.
    /// </summary>
    /// <returns>The context, which needs to be disposed, and the game configuration.</returns>
    public static async Task<(IContext Context, GameConfiguration Configuration)> CreateAsync()
    {
        // The plugin manager of the initialization only discovers the plugins of loaded assemblies.
        _ = ContentStagesPlugIn.TypeId;

        var provider = new InMemoryPersistenceContextProvider();
        await new Persistence.Initialization.VersionSeasonSix.DataInitialization(provider, NullLoggerFactory.Instance).CreateInitialDataAsync(1, false).ConfigureAwait(false);
        var context = provider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        return (context, configuration);
    }

    /// <summary>
    /// Creates a configuration with the default releases.
    /// </summary>
    /// <param name="releasedIds">The ids of the releases which are released.</param>
    /// <returns>The configuration.</returns>
    public static ContentStageConfiguration CreateConfiguration(params string[] releasedIds)
    {
        var configuration = new ContentStageConfiguration { Releases = DefaultReleases.Create() };
        foreach (var id in releasedIds)
        {
            configuration.FindRelease(id)!.ReleasedAt = new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc);
        }

        return configuration;
    }

    /// <summary>
    /// Describes all values which the <see cref="ContentStageApplier"/> may change, to compare them.
    /// </summary>
    /// <param name="configuration">The game configuration.</param>
    /// <returns>The values, one per line.</returns>
    public static List<string> DescribeLockableValues(GameConfiguration configuration)
    {
        var values = new List<string>();
        values.AddRange(configuration.Items.Select(i => $"item {i.Group}:{i.Number} {i.DropsFromMonsters} {i.MaximumSockets}"));
        values.AddRange(configuration.CharacterClasses.Select(c => $"class {c.Number} {c.CanGetCreated}"));
        values.AddRange(configuration.Monsters.SelectMany(m => m.Quests.Select(q => $"quest {m.Number}:{q.Group}:{q.Number}:{q.QualifiedCharacter?.Number} {q.MinimumCharacterLevel}")));
        values.AddRange(configuration.MiniGameDefinitions.Select(g => $"minigame {g.Type}:{g.GameLevel} {g.MinimumCharacterLevel} {g.MinimumSpecialCharacterLevel}"));
        values.AddRange(configuration.Maps.SelectMany(m => m.MonsterSpawns.Select(a => $"spawn {m.Number} {a.MonsterDefinition?.Number} {a.X1}:{a.Y1} {a.Quantity}")));
        values.AddRange(configuration.PlugInConfigurations.Select(p => $"plugin {p.TypeId} {p.IsActive}"));
        values.Sort(StringComparer.Ordinal);
        return values;
    }
}
