// <copyright file="DefaultReleasesTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// Tests the <see cref="DefaultReleases"/> against the season 6 data.
/// </summary>
[TestFixture]
public class DefaultReleasesTest
{
    private IContext _context = null!;
    private GameConfiguration _configuration = null!;

    /// <summary>
    /// Creates the season 6 data.
    /// </summary>
    /// <returns>The task.</returns>
    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        (this._context, this._configuration) = await ContentStageTestData.CreateAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes the context.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
        this._context.Dispose();
    }

    /// <summary>
    /// Tests that the content of the default releases exists, that the item ranges aren't empty and that no content is part of two releases.
    /// </summary>
    [Test]
    public void DefaultReleasesAreValid()
    {
        Assert.That(ContentReleaseValidator.Validate(DefaultReleases.Create(), this._configuration), Is.Empty);
    }

    /// <summary>
    /// Tests that the NPCs of the default releases are spawned somewhere, so locking them has an effect.
    /// </summary>
    [Test]
    public void NpcsAreSpawned()
    {
        var spawnedNumbers = this._configuration.Maps.SelectMany(m => m.MonsterSpawns).Select(a => a.MonsterDefinition?.Number).OfType<short>();
        Assert.That(DefaultReleases.Create().SelectMany(r => r.Content.Npcs), Is.SubsetOf(spawnedNumbers));
    }

    /// <summary>
    /// Tests that all items with sockets come with the socket system.
    /// </summary>
    [Test]
    public void SocketItemsComeWithTheSocketSystem()
    {
        var locks = ContentStageTestData.CreateConfiguration().CreateLocks();
        var releases = this._configuration.Items.Where(i => i.MaximumSockets > 0).Select(i => locks.GetRelease(i)?.Id).Distinct();
        Assert.That(releases, Is.EqualTo(new[] { "sistema-de-sockets" }));
    }

    /// <summary>
    /// Tests which releases lock the mini games.
    /// </summary>
    /// <param name="type">The type of the mini game.</param>
    /// <param name="level">The level of the mini game.</param>
    /// <returns>The id of the release which locks the mini game.</returns>
    [TestCase(MiniGameType.BloodCastle, 7, ExpectedResult = null)]
    [TestCase(MiniGameType.BloodCastle, 8, ExpectedResult = "tercera-evolucion")]
    [TestCase(MiniGameType.DevilSquare, 7, ExpectedResult = "tercera-evolucion")]
    [TestCase(MiniGameType.ChaosCastle, 7, ExpectedResult = "tercera-evolucion")]
    [TestCase(MiniGameType.Kanturu, 1, ExpectedResult = null)]
    [TestCase(MiniGameType.Doppelganger, 1, ExpectedResult = "doppelganger")]
    [TestCase(MiniGameType.ImperialGuardian, 1, ExpectedResult = "imperial-guardian")]
    public string? MiniGameReleases(MiniGameType type, int level)
    {
        var miniGame = this._configuration.MiniGameDefinitions.Single(g => g.Type == type && g.GameLevel == level);
        return ContentStageTestData.CreateConfiguration().CreateLocks().GetRelease(miniGame)?.Id;
    }

    /// <summary>
    /// Tests that the ids of the default releases are unique and stay as they are, because the stored configurations refer to them.
    /// </summary>
    [Test]
    public void IdsAreStable()
    {
        Assert.That(DefaultReleases.Create().Select(r => r.Id), Is.EqualTo(new[]
        {
            "tercera-evolucion", "swamp-of-calmness",
            "sistema-de-sockets", "armas-y-sets-s4", "raklion", "vulcanus", "santa-village", "moss-el-mercader",
            "gens", "cash-shop", "duel-arena", "doppelganger", "imperial-guardian", "loren-market", "quests-elf-soldier",
            "rage-fighter", "karutan", "mercaderes-s6",
        }));
    }
}
