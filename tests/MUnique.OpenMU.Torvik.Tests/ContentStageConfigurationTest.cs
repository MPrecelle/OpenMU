// <copyright file="ContentStageConfigurationTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// Tests how the <see cref="ContentStageConfiguration"/> is stored, and that the configurations from before the releases are migrated.
/// </summary>
[TestFixture]
public class ContentStageConfigurationTest
{
    /// <summary>
    /// Tests that a configuration of the classic stage gets the default releases, all locked, and keeps its snapshot.
    /// </summary>
    [Test]
    public void ClassicStageGetsLockedDefaultReleases()
    {
        var configuration = Load("""{"CurrentStage":0,"Snapshot":{"CharacterClasses":{"24":true}}}""");

        Assert.That(configuration.Releases.Select(r => r.Id), Is.EqualTo(DefaultReleases.Create().Select(r => r.Id)));
        Assert.That(configuration.Releases.Select(r => r.IsReleased), Is.All.False);
        Assert.That(configuration.Snapshot!.CharacterClasses[24], Is.True);
        Assert.That(configuration.LegacyStage, Is.Null);
    }

    /// <summary>
    /// Tests that the releases of the reached stage are released.
    /// </summary>
    [Test]
    public void ReachedStageIsReleased()
    {
        var configuration = Load("""{"CurrentStage":2}""");

        Assert.That(configuration.Releases.Where(r => r.IsReleased).Select(r => r.Era).Distinct(), Is.EquivalentTo(new[] { ContentStage.Season3, ContentStage.Season4 }));
        Assert.That(configuration.FindRelease("gens")!.IsReleased, Is.False);
    }

    /// <summary>
    /// Tests that a stored configuration keeps its releases and doesn't get the old stage again.
    /// </summary>
    [Test]
    public void RoundTripKeepsReleases()
    {
        var configuration = ContentStageTestData.CreateConfiguration("gens");
        configuration.FindRelease("raklion")!.ScheduledAt = new DateTime(2027, 3, 1, 23, 0, 0, DateTimeKind.Utc);
        var plugInConfiguration = new PlugInConfiguration();
        plugInConfiguration.SetConfiguration(configuration, null);

        var loaded = plugInConfiguration.GetConfiguration<ContentStageConfiguration>(null)!;

        Assert.That(plugInConfiguration.CustomConfiguration, Does.Not.Contain("CurrentStage"));
        Assert.That(loaded.FindRelease("gens")!.IsReleased, Is.True);
        Assert.That(loaded.FindRelease("raklion")!.ScheduledAt, Is.EqualTo(configuration.FindRelease("raklion")!.ScheduledAt));
        Assert.That(loaded.FindRelease("doppelganger")!.Content.MiniGames, Is.EqualTo(configuration.FindRelease("doppelganger")!.Content.MiniGames));
    }

    /// <summary>
    /// Tests that the game servers can load the configuration, which they do with the reference handler of their data source.
    /// </summary>
    [Test]
    public void LoadsWithTheReferenceHandlerOfTheGameServers()
    {
        var configuration = ContentStageTestData.CreateConfiguration("gens");
        var plugInConfiguration = new PlugInConfiguration();
        plugInConfiguration.SetConfiguration(configuration, null);
        var referenceHandler = new ByDataSourceReferenceHandler(Mock.Of<IDataSource<GameConfiguration>>());

        var loaded = (ContentStageConfiguration)plugInConfiguration.GetConfiguration(typeof(ContentStageConfiguration), referenceHandler)!;

        Assert.That(loaded.Releases.Select(r => r.Id), Is.EqualTo(configuration.Releases.Select(r => r.Id)));
        Assert.That(loaded.CreateLocks().GetRelease(new GameMapDefinition { Number = 63 })?.Id, Is.EqualTo("vulcanus"));
        Assert.That(loaded.FindRelease("doppelganger")!.Content.MiniGames, Is.EqualTo(configuration.FindRelease("doppelganger")!.Content.MiniGames));
    }

    /// <summary>
    /// Tests that a new configuration has no releases, so a plugin without a stored configuration doesn't lock anything.
    /// </summary>
    [Test]
    public void NewConfigurationLocksNothing()
    {
        Assert.That(new ContentStageConfiguration().Releases, Is.Empty);
    }

    private static ContentStageConfiguration Load(string json)
    {
        var plugInConfiguration = new PlugInConfiguration { CustomConfiguration = json };
        return plugInConfiguration.GetConfiguration<ContentStageConfiguration>(null)!;
    }
}
