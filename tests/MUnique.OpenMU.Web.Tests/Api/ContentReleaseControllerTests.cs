// <copyright file="ContentReleaseControllerTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Api;

using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.Torvik.ContentStages;
using MUnique.OpenMU.Web.AdminPanel.API;

/// <summary>
/// Tests for the <see cref="ContentReleaseController"/>, which shows, edits, schedules and releases the content releases.
/// </summary>
[TestFixture]
public class ContentReleaseControllerTests
{
    private ContentReleaseController _controller = null!;

    /// <summary>
    /// Sets up an in-memory database with a game configuration which has the map 63, and the controller.
    /// </summary>
    /// <returns>The task.</returns>
    [SetUp]
    public async Task SetUpAsync()
    {
        var persistenceContextProvider = new InMemoryPersistenceContextProvider();
        using (var context = persistenceContextProvider.CreateNewContext())
        {
            var gameConfiguration = context.CreateNew<GameConfiguration>();
            var map = context.CreateNew<GameMapDefinition>();
            map.Number = 63;
            gameConfiguration.Maps.Add(map);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        this._controller = new ContentReleaseController(persistenceContextProvider, Mock.Of<ILogger<ContentReleaseController>>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
    }

    /// <summary>
    /// A valid release is created locked, and returned by the list.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task CreateAsync()
    {
        var result = await this._controller.CreateAsync(CreateVulcanus(), CancellationToken.None).ConfigureAwait(false);

        Assert.That(result, Is.InstanceOf<CreatedResult>());
        Assert.That(((ReleaseView)((CreatedResult)result).Value!).Status, Is.EqualTo(ReleaseStatus.Locked));
        var all = (OkObjectResult)(await this._controller.GetAllAsync(CancellationToken.None).ConfigureAwait(false)).Result!;
        Assert.That(((IEnumerable<ReleaseView>)all.Value!).Select(r => r.Id), Is.EqualTo(new[] { "vulcanus" }));
    }

    /// <summary>
    /// An invalid release is answered with the errors, and isn't created.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task CreateInvalidAsync()
    {
        var release = CreateVulcanus();
        release.Content.Maps.Add(999);

        var result = await this._controller.CreateAsync(release, CancellationToken.None).ConfigureAwait(false);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        Assert.That(((BadRequestObjectResult)result).Value, Is.EqualTo(new[] { new ReleaseValidationError(ReleaseValidationErrorCode.UnknownContent, "vulcanus", "map 999") }));
        Assert.That((await this._controller.GetAsync("vulcanus", CancellationToken.None).ConfigureAwait(false)).Result, Is.InstanceOf<NotFoundResult>());
    }

    /// <summary>
    /// A release gets scheduled with an offset, which is stored in UTC, and then released.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task ScheduleAndReleaseAsync()
    {
        await this._controller.CreateAsync(CreateVulcanus(), CancellationToken.None).ConfigureAwait(false);
        var scheduledAt = new DateTimeOffset(DateTime.UtcNow.Year + 1, 3, 1, 20, 0, 0, TimeSpan.FromHours(-3));

        var scheduled = (ReleaseView)((OkObjectResult)await this._controller.ScheduleAsync("vulcanus", new ReleaseSchedule(scheduledAt), CancellationToken.None).ConfigureAwait(false)).Value!;
        var released = (ReleaseView)((OkObjectResult)await this._controller.ReleaseAsync("vulcanus", CancellationToken.None).ConfigureAwait(false)).Value!;

        Assert.That(scheduled.Status, Is.EqualTo(ReleaseStatus.Scheduled));
        Assert.That(scheduled.ScheduledAt, Is.EqualTo(scheduledAt.UtcDateTime));
        Assert.That(scheduled.ScheduledAt!.Value.Hour, Is.EqualTo(23));
        Assert.That(released.Status, Is.EqualTo(ReleaseStatus.Released));
        Assert.That(released.ScheduledAt, Is.Null);
    }

    /// <summary>
    /// Changes of unknown releases are answered with not found.
    /// </summary>
    /// <returns>The task.</returns>
    [Test]
    public async Task UnknownReleaseAsync()
    {
        Assert.That(await this._controller.ReleaseAsync("vulcanus", CancellationToken.None).ConfigureAwait(false), Is.InstanceOf<NotFoundResult>());
        Assert.That(await this._controller.DeleteAsync("vulcanus", CancellationToken.None).ConfigureAwait(false), Is.InstanceOf<NotFoundResult>());
    }

    /// <summary>
    /// Reverting a release needs the administrator role, because the players may already have its items.
    /// </summary>
    [Test]
    public void RevertNeedsAdministrator()
    {
        var policies = typeof(ContentReleaseController).GetMethod(nameof(ContentReleaseController.RevertAsync))!
            .GetCustomAttributes<AuthorizeAttribute>()
            .Select(a => a.Policy);

        Assert.That(policies, Does.Contain("OpenMU.Administrator"));
    }

    private static ContentRelease CreateVulcanus()
    {
        var release = new ContentRelease { Id = "vulcanus", Name = "Vulcanus", Era = ContentStage.Season4 };
        release.Content.Maps.Add(63);
        return release;
    }
}
