// <copyright file="ContentReleaseServiceTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.Tests;

using System.Text.Json;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Gens;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// Tests the <see cref="ContentReleaseService"/> with the season 6 data and the default releases.
/// </summary>
[TestFixture]
public class ContentReleaseServiceTest
{
    private static readonly DateTime Now = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

    private IContext _context = null!;
    private GameConfiguration _configuration = null!;
    private ContentReleaseService _service = null!;

    /// <summary>
    /// Creates the season 6 data with the locked default releases.
    /// </summary>
    /// <returns>The task.</returns>
    [SetUp]
    public async Task SetUpAsync()
    {
        (this._context, this._configuration) = await ContentStageTestData.CreateAsync().ConfigureAwait(false);
        new ContentStageApplier(this._context).Apply(this._configuration, ContentStageTestData.CreateConfiguration());
        this._service = new ContentReleaseService(this._context, this._configuration);
    }

    /// <summary>
    /// Disposes the context.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        this._context.Dispose();
    }

    /// <summary>
    /// Tests that releasing unlocks the content in the data and stores the release.
    /// </summary>
    [Test]
    public void ReleaseUnlocksContent()
    {
        Assert.That(this.Spawns(543).Select(a => a.Quantity), Is.All.Zero, "Gens Duprian");

        var errors = this._service.Release("gens", Now);

        Assert.That(errors, Is.Empty);
        Assert.That(this.Spawns(543).Select(a => a.Quantity), Is.All.Positive, "Gens Duprian");
        Assert.That(this._configuration.PlugInConfigurations.Single(p => p.TypeId == typeof(GensFeaturePlugIn).GUID).IsActive, Is.True);
        var stored = ContentStageApplier.GetConfiguration(this._configuration).FindRelease("gens")!;
        Assert.That(stored.ReleasedAt, Is.EqualTo(Now));
    }

    /// <summary>
    /// Tests that a release can't be released twice.
    /// </summary>
    [Test]
    public void ReleaseTwiceFails()
    {
        this._service.Release("gens", Now);

        Assert.That(this._service.Release("gens", Now), Is.EqualTo(new[] { new ReleaseValidationError(ReleaseValidationErrorCode.AlreadyReleased, "gens") }));
    }

    /// <summary>
    /// Tests that a release can't be released before the releases it requires.
    /// </summary>
    [Test]
    public void ReleaseNeedsRequiredReleases()
    {
        this.RequireGensForCashShop();

        var errors = this._service.Release("cash-shop", Now);

        Assert.That(errors, Is.EqualTo(new[] { new ReleaseValidationError(ReleaseValidationErrorCode.RequirementNotReleased, "cash-shop", "gens") }));
        Assert.That(this._service.Configuration.FindRelease("cash-shop")!.IsReleased, Is.False);
    }

    /// <summary>
    /// Tests that a scheduled release needs its requirements scheduled before it, also when the requirement gets rescheduled.
    /// </summary>
    [Test]
    public void ScheduleNeedsRequirementsBefore()
    {
        this.RequireGensForCashShop();

        Assert.That(this._service.Schedule("cash-shop", Now.AddDays(7), Now).Single().Code, Is.EqualTo(ReleaseValidationErrorCode.RequirementNotScheduledBefore));
        Assert.That(this._service.Schedule("gens", Now.AddDays(1), Now), Is.Empty);
        Assert.That(this._service.Schedule("cash-shop", Now.AddDays(7), Now), Is.Empty);
        Assert.That(this._service.Schedule("gens", Now.AddDays(8), Now), Is.EqualTo(new[] { new ReleaseValidationError(ReleaseValidationErrorCode.RequirementNotScheduledBefore, "cash-shop", "gens") }));
        Assert.That(this._service.Schedule("gens", null, Now).Single().Code, Is.EqualTo(ReleaseValidationErrorCode.RequirementNotScheduledBefore));
        Assert.That(this._service.Configuration.FindRelease("gens")!.ScheduledAt, Is.EqualTo(Now.AddDays(1)));
    }

    /// <summary>
    /// Tests that releasing after the countdown schedules the release at its end, and keeps the content locked until then.
    /// </summary>
    [Test]
    public void ReleaseAfterCountdownSchedules()
    {
        Assert.That(this._service.ReleaseAfterCountdown("gens", Now), Is.Empty);

        var gens = this._service.Configuration.FindRelease("gens")!;
        Assert.That(gens.ScheduledAt, Is.EqualTo(Now.AddMinutes(this._service.Configuration.ReleaseNowMinutes)));
        Assert.That(gens.IsReleased, Is.False);
        Assert.That(this.Spawns(543).Select(a => a.Quantity), Is.All.Zero, "Gens Duprian");
    }

    /// <summary>
    /// Tests that a release can't be scheduled in the past.
    /// </summary>
    [Test]
    public void ScheduleInThePastFails()
    {
        Assert.That(this._service.Schedule("gens", Now.AddMinutes(-1), Now).Single().Code, Is.EqualTo(ReleaseValidationErrorCode.ScheduledInThePast));
    }

    /// <summary>
    /// Tests that releasing removes the schedule.
    /// </summary>
    [Test]
    public void ReleaseRemovesSchedule()
    {
        this._service.Schedule("gens", Now.AddDays(1), Now);

        this._service.Release("gens", Now);

        Assert.That(this._service.Configuration.FindRelease("gens")!.ScheduledAt, Is.Null);
    }

    /// <summary>
    /// Tests that a new release locks its content.
    /// </summary>
    [Test]
    public void CreateLocksContent()
    {
        var kris = this._configuration.Items.Single(i => i.Group == 0 && i.Number == 0);
        var release = new ContentRelease { Id = "kris", Name = "Kris", ReleasedAt = Now };
        release.Content.Items.Add(new ReleaseContent.ItemRange(0, 0, 0));

        Assert.That(this._service.Create(release), Is.Empty);
        Assert.That(kris.DropsFromMonsters, Is.False);
        Assert.That(this._service.Configuration.FindRelease("kris")!.IsReleased, Is.False);
    }

    /// <summary>
    /// Tests that the content of a new release can't be part of another release.
    /// </summary>
    [Test]
    public void CreateWithOverlappingContentFails()
    {
        var release = new ContentRelease { Id = "seeds", Name = "Seeds" };
        release.Content.Items.Add(new ReleaseContent.ItemRange(12, 70, 80));

        Assert.That(this._service.Create(release), Is.EqualTo(new[] { new ReleaseValidationError(ReleaseValidationErrorCode.OverlappingContent, "seeds", "items 12:70-80 (sistema-de-sockets)") }));
        Assert.That(this._service.Configuration.FindRelease("seeds"), Is.Null);
    }

    /// <summary>
    /// Tests that a new release needs a valid and unique id, a name and existing content.
    /// </summary>
    [Test]
    public void CreateValidatesTheRelease()
    {
        var release = new ContentRelease { Id = "Gens", Name = " " };
        release.Content.Maps.Add(999);

        var codes = this._service.Create(release).Select(e => e.Code);

        Assert.That(codes, Is.EquivalentTo(new[] { ReleaseValidationErrorCode.InvalidId, ReleaseValidationErrorCode.MissingName, ReleaseValidationErrorCode.UnknownContent }));
        Assert.That(this._service.Create(new ContentRelease { Id = "gens", Name = "Gens" }).Single().Code, Is.EqualTo(ReleaseValidationErrorCode.DuplicateId));
    }

    /// <summary>
    /// Tests that releases can't require each other.
    /// </summary>
    [Test]
    public void CyclicRequirementFails()
    {
        this.RequireGensForCashShop();
        var gens = this.Copy("gens");
        gens.Requires.Add("cash-shop");

        Assert.That(this._service.Update("gens", gens).Select(e => e.Code), Is.EqualTo(new[] { ReleaseValidationErrorCode.CyclicRequirement, ReleaseValidationErrorCode.CyclicRequirement }));
    }

    /// <summary>
    /// Tests that the content of a released release can't change anymore, but its name can.
    /// </summary>
    [Test]
    public void UpdateOfReleasedReleaseOnlyChangesTheName()
    {
        this._service.Release("vulcanus", Now);
        var vulcanus = this.Copy("vulcanus");
        vulcanus.Name = "Vulcanus PvP";

        Assert.That(this._service.Update("vulcanus", vulcanus), Is.Empty);
        vulcanus.Content.Maps.Add(62);
        Assert.That(this._service.Update("vulcanus", vulcanus).Single().Code, Is.EqualTo(ReleaseValidationErrorCode.AlreadyReleased));
        Assert.That(this._service.Configuration.FindRelease("vulcanus")!.Name, Is.EqualTo("Vulcanus PvP"));
    }

    /// <summary>
    /// Tests that removing content from a release unlocks it.
    /// </summary>
    [Test]
    public void UpdateUnlocksRemovedContent()
    {
        var gens = this.Copy("gens");
        gens.Content.Npcs.Remove(543);

        Assert.That(this._service.Update("gens", gens), Is.Empty);
        Assert.That(this.Spawns(543).Select(a => a.Quantity), Is.All.Positive, "Gens Duprian");
        Assert.That(this.Spawns(544).Select(a => a.Quantity), Is.All.Zero, "Gens Vanert");
    }

    /// <summary>
    /// Tests that deleting a release unlocks its content, and that released or required releases can't be deleted.
    /// </summary>
    [Test]
    public void Delete()
    {
        this.RequireGensForCashShop();
        this._service.Release("vulcanus", Now);

        Assert.That(this._service.Delete("vulcanus").Single().Code, Is.EqualTo(ReleaseValidationErrorCode.AlreadyReleased));
        Assert.That(this._service.Delete("gens").Single().Code, Is.EqualTo(ReleaseValidationErrorCode.UnknownRequirement));
        Assert.That(this._service.Delete("cash-shop"), Is.Empty);
        Assert.That(this._service.Delete("gens"), Is.Empty);
        Assert.That(this.Spawns(543).Select(a => a.Quantity), Is.All.Positive, "Gens Duprian");
    }

    /// <summary>
    /// Tests that reverting locks the content again, unless a released release requires it.
    /// </summary>
    [Test]
    public void Revert()
    {
        this.RequireGensForCashShop();
        this._service.Release("gens", Now);
        this._service.Release("cash-shop", Now);

        Assert.That(this._service.Revert("gens").Single().Code, Is.EqualTo(ReleaseValidationErrorCode.RequirementNotReleased));
        Assert.That(this._service.Revert("cash-shop"), Is.Empty);
        Assert.That(this._service.Revert("gens"), Is.Empty);
        Assert.That(this._service.Revert("gens").Single().Code, Is.EqualTo(ReleaseValidationErrorCode.NotReleased));
        Assert.That(this.Spawns(543).Select(a => a.Quantity), Is.All.Zero, "Gens Duprian");
    }

    /// <summary>
    /// Tests that the missing default releases are added, locked, and the existing ones stay as they are.
    /// </summary>
    [Test]
    public void AddMissingDefaults()
    {
        this._service.Release("gens", Now);
        this._service.Delete("karutan");

        Assert.That(this._service.AddMissingDefaults(), Is.Empty);
        Assert.That(this._service.Configuration.Releases, Has.Count.EqualTo(DefaultReleases.Create().Count));
        Assert.That(this._service.Configuration.FindRelease("gens")!.IsReleased, Is.True);
        Assert.That(this._service.Configuration.FindRelease("karutan")!.IsReleased, Is.False);
    }

    private void RequireGensForCashShop()
    {
        var cashShop = this.Copy("cash-shop");
        cashShop.Requires.Add("gens");
        Assert.That(this._service.Update("cash-shop", cashShop), Is.Empty);
    }

    private ContentRelease Copy(string id)
        => JsonSerializer.Deserialize<ContentRelease>(JsonSerializer.Serialize(this._service.Configuration.FindRelease(id)))!;

    private IEnumerable<MonsterSpawnArea> Spawns(short monsterNumber) => this._configuration.Maps.SelectMany(m => m.MonsterSpawns).Where(a => a.MonsterDefinition?.Number == monsterNumber);
}
