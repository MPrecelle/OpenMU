// <copyright file="ReleaseCountdownTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.Tests;

using MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// Tests the <see cref="ReleaseCountdown"/>, which decides when the players get warned and when the releases are due.
/// </summary>
[TestFixture]
public class ReleaseCountdownTest
{
    private static readonly DateTime Start = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ReleaseAt = Start.AddHours(2);

    /// <summary>
    /// Tests that each warning is sent once, when its minutes are reached, and that the release is due at its point in time.
    /// </summary>
    [Test]
    public void WarnsOnceAtEachMinutesAndThenReleases()
    {
        var configuration = CreateConfiguration(("vulcanus", ReleaseAt));
        var countdown = new ReleaseCountdown(Start);

        var steps = new[] { -61, -60, -59.9, -15, -14, -5, -1, -0.5, 0 }
            .Select(minutes => countdown.Next(configuration, ReleaseAt.AddMinutes(minutes)))
            .ToList();

        Assert.That(steps.Select(step => step.Warnings.SingleOrDefault()?.Minutes), Is.EqualTo(new int?[] { null, 60, null, 15, null, 5, 1, null, null }));
        Assert.That(steps.Select(step => step.Due.Count), Is.EqualTo(new[] { 0, 0, 0, 0, 0, 0, 0, 0, 1 }));
    }

    /// <summary>
    /// Tests that a release which is scheduled shortly before gets only one warning with the remaining minutes.
    /// </summary>
    [Test]
    public void ShortCountdownGetsOneWarning()
    {
        var now = Start.AddHours(1);
        var configuration = CreateConfiguration(("vulcanus", now.AddMinutes(3)));
        var countdown = new ReleaseCountdown(Start);

        var first = countdown.Next(configuration, now);
        var second = countdown.Next(configuration, now.AddSeconds(15));

        Assert.That(first.Warnings.Single().Minutes, Is.EqualTo(3));
        Assert.That(second.Warnings, Is.Empty);
    }

    /// <summary>
    /// Tests that the releases of the same point in time are warned about together.
    /// </summary>
    [Test]
    public void ReleasesAtTheSameTimeAreWarnedTogether()
    {
        var configuration = CreateConfiguration(("vulcanus", ReleaseAt), ("gens", ReleaseAt), ("raklion", ReleaseAt.AddDays(1)));
        var countdown = new ReleaseCountdown(Start);

        var step = countdown.Next(configuration, ReleaseAt.AddMinutes(-60));

        Assert.That(step.Warnings.Single().Releases.Select(r => r.Id), Is.EquivalentTo(new[] { "vulcanus", "gens" }));
    }

    /// <summary>
    /// Tests that a release which became due while the server was down waits for the countdown of a release now, with its warnings.
    /// </summary>
    [Test]
    public void DueReleaseWaitsForTheCountdownAfterTheStart()
    {
        var configuration = CreateConfiguration(("vulcanus", Start.AddDays(-1)));
        var countdown = new ReleaseCountdown(Start);

        var atStart = countdown.Next(configuration, Start);
        var beforeEnd = countdown.Next(configuration, Start.AddMinutes(4.9));
        var atEnd = countdown.Next(configuration, Start.AddMinutes(configuration.ReleaseNowMinutes));

        Assert.That(atStart.Warnings.Single().Minutes, Is.EqualTo(5));
        Assert.That(atStart.Due, Is.Empty);
        Assert.That(beforeEnd.Warnings.Single().Minutes, Is.EqualTo(1));
        Assert.That(atEnd.Due.Single().Id, Is.EqualTo("vulcanus"));
    }

    /// <summary>
    /// Tests that released and not scheduled releases are ignored.
    /// </summary>
    [Test]
    public void IgnoresReleasedAndNotScheduledReleases()
    {
        var configuration = CreateConfiguration(("vulcanus", ReleaseAt), ("gens", null));
        configuration.FindRelease("vulcanus")!.ReleasedAt = Start;
        var countdown = new ReleaseCountdown(Start);

        var step = countdown.Next(configuration, ReleaseAt);

        Assert.That(step.Warnings, Is.Empty);
        Assert.That(step.Due, Is.Empty);
    }

    /// <summary>
    /// Tests that a release which failed isn't due again, until it's scheduled again.
    /// </summary>
    [Test]
    public void FailedReleaseIsDueAfterItsScheduledAgain()
    {
        var configuration = CreateConfiguration(("vulcanus", ReleaseAt));
        var countdown = new ReleaseCountdown(Start);
        countdown.MarkAsFailed(configuration.FindRelease("vulcanus")!);

        var failed = countdown.Next(configuration, ReleaseAt);
        configuration.FindRelease("vulcanus")!.ScheduledAt = ReleaseAt.AddHours(1);
        var rescheduled = countdown.Next(configuration, ReleaseAt.AddHours(1));

        Assert.That(failed.Due, Is.Empty);
        Assert.That(rescheduled.Due.Single().Id, Is.EqualTo("vulcanus"));
    }

    /// <summary>
    /// Tests that a rescheduled release gets its warnings again.
    /// </summary>
    [Test]
    public void RescheduledReleaseIsWarnedAgain()
    {
        var configuration = CreateConfiguration(("vulcanus", ReleaseAt));
        var countdown = new ReleaseCountdown(Start);
        countdown.Next(configuration, ReleaseAt.AddMinutes(-15));

        configuration.FindRelease("vulcanus")!.ScheduledAt = ReleaseAt.AddHours(1);
        var step = countdown.Next(configuration, ReleaseAt.AddMinutes(45));

        Assert.That(step.Warnings.Single().Minutes, Is.EqualTo(15));
    }

    private static ContentStageConfiguration CreateConfiguration(params (string Id, DateTime? ScheduledAt)[] releases)
    {
        var configuration = new ContentStageConfiguration();
        configuration.Releases.AddRange(releases.Select(r => new ContentRelease { Id = r.Id, Name = r.Id, ScheduledAt = r.ScheduledAt }));
        return configuration;
    }
}
