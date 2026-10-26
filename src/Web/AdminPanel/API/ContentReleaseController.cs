// <copyright file="ContentReleaseController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Torvik.ContentStages;
using MUnique.OpenMU.Web.AdminPanel.Auth;

/// <summary>
/// Torvik MU: the API to show, edit, schedule and release the <see cref="ContentRelease"/>s, which lock content until they are released.
/// </summary>
/// <remarks>
/// A change which isn't valid returns <c>400</c> with the <see cref="ReleaseValidationError"/>s and changes nothing.
/// The game servers apply the changed data after "Reload configuration and restart" in the admin panel.
/// </remarks>
[ApiController]
[Route("api/releases")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.ApiSchemes, Policy = AdminPolicies.Operator)]
public class ContentReleaseController : ControllerBase
{
    private readonly IPersistenceContextProvider _persistenceContextProvider;
    private readonly ILogger<ContentReleaseController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentReleaseController"/> class.
    /// </summary>
    /// <param name="persistenceContextProvider">The persistence context provider.</param>
    /// <param name="logger">The logger.</param>
    public ContentReleaseController(IPersistenceContextProvider persistenceContextProvider, ILogger<ContentReleaseController> logger)
    {
        this._persistenceContextProvider = persistenceContextProvider;
        this._logger = logger;
    }

    /// <summary>
    /// Gets the releases, as stored in the database.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The releases.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReleaseView>>> GetAllAsync(CancellationToken cancellationToken)
    {
        using var context = this._persistenceContextProvider.CreateNewContext();
        var gameConfiguration = await GetGameConfigurationAsync(context, cancellationToken).ConfigureAwait(false);
        return this.Ok(ContentStageApplier.GetConfiguration(gameConfiguration).Releases.Select(ReleaseView.From));
    }

    /// <summary>
    /// Gets a release.
    /// </summary>
    /// <param name="id">The id of the release.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The release; <c>404</c>, if it doesn't exist.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ReleaseView>> GetAsync(string id, CancellationToken cancellationToken)
    {
        using var context = this._persistenceContextProvider.CreateNewContext();
        var gameConfiguration = await GetGameConfigurationAsync(context, cancellationToken).ConfigureAwait(false);
        return ContentStageApplier.GetConfiguration(gameConfiguration).FindRelease(id) is { } release
            ? ReleaseView.From(release)
            : this.NotFound();
    }

    /// <summary>
    /// Creates a release. It's locked and not scheduled.
    /// </summary>
    /// <param name="release">The release.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created release; <c>400</c> with the errors, if it isn't valid.</returns>
    [HttpPost]
    public Task<ActionResult> CreateAsync([FromBody] ContentRelease release, CancellationToken cancellationToken)
        => this.ChangeAsync(release.Id, false, service => service.Create(release), "created", view => this.Created($"api/releases/{view!.Id}", view), cancellationToken);

    /// <summary>
    /// Changes the name, description, era, requirements and content of a release.
    /// Of a released release, only the name and the description can change.
    /// </summary>
    /// <param name="id">The id of the release.</param>
    /// <param name="release">The changed release. Its id and points in time are ignored.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The changed release; <c>400</c> with the errors, if the change isn't valid; <c>404</c>, if it doesn't exist.</returns>
    [HttpPut("{id}")]
    public Task<ActionResult> UpdateAsync(string id, [FromBody] ContentRelease release, CancellationToken cancellationToken)
        => this.ChangeAsync(id, true, service => service.Update(id, release), "changed", this.Ok, cancellationToken);

    /// <summary>
    /// Deletes a release which isn't released.
    /// </summary>
    /// <param name="id">The id of the release.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>204</c>; <c>400</c> with the errors, if it's released or required by others; <c>404</c>, if it doesn't exist.</returns>
    [HttpDelete("{id}")]
    public Task<ActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
        => this.ChangeAsync(id, true, service => service.Delete(id), "deleted", _ => this.NoContent(), cancellationToken);

    /// <summary>
    /// Schedules a release which isn't released.
    /// </summary>
    /// <param name="id">The id of the release.</param>
    /// <param name="schedule">The point in time of the release.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The scheduled release; <c>400</c> with the errors, if it can't be scheduled then; <c>404</c>, if it doesn't exist.</returns>
    [HttpPut("{id}/schedule")]
    public Task<ActionResult> ScheduleAsync(string id, [FromBody] ReleaseSchedule schedule, CancellationToken cancellationToken)
        => this.ChangeAsync(id, true, service => service.Schedule(id, schedule.ScheduledAt.UtcDateTime, DateTime.UtcNow), "scheduled", this.Ok, cancellationToken);

    /// <summary>
    /// Removes the schedule of a release which isn't released.
    /// </summary>
    /// <param name="id">The id of the release.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The release; <c>400</c> with the errors, e.g. if scheduled releases require it; <c>404</c>, if it doesn't exist.</returns>
    [HttpDelete("{id}/schedule")]
    public Task<ActionResult> UnscheduleAsync(string id, CancellationToken cancellationToken)
        => this.ChangeAsync(id, true, service => service.Schedule(id, null, DateTime.UtcNow), "unscheduled", this.Ok, cancellationToken);

    /// <summary>
    /// Releases the content of a release now.
    /// </summary>
    /// <param name="id">The id of the release.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The released release; <c>400</c> with the errors, e.g. if a required release isn't released; <c>404</c>, if it doesn't exist.</returns>
    [HttpPost("{id}/release")]
    public Task<ActionResult> ReleaseAsync(string id, CancellationToken cancellationToken)
        => this.ChangeAsync(id, true, service => service.Release(id, DateTime.UtcNow), "released", this.Ok, cancellationToken);

    /// <summary>
    /// Locks the content of a released release again. The players may already have its items, so it needs the administrator role.
    /// </summary>
    /// <param name="id">The id of the release.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The reverted release; <c>400</c> with the errors, e.g. if released releases require it; <c>404</c>, if it doesn't exist.</returns>
    [HttpPost("{id}/revert")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.ApiSchemes, Policy = AdminPolicies.Administrator)]
    public Task<ActionResult> RevertAsync(string id, CancellationToken cancellationToken)
        => this.ChangeAsync(id, true, service => service.Revert(id), "reverted", this.Ok, cancellationToken);

    /// <summary>
    /// Adds the default releases whose ids don't exist yet. They are locked.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>All releases; <c>400</c> with the errors, e.g. if their content is part of other releases.</returns>
    [HttpPost("defaults")]
    public async Task<ActionResult<IEnumerable<ReleaseView>>> AddMissingDefaultsAsync(CancellationToken cancellationToken)
    {
        using var context = this._persistenceContextProvider.CreateNewContext();
        var service = new ContentReleaseService(context, await GetGameConfigurationAsync(context, cancellationToken).ConfigureAwait(false));
        var errors = service.AddMissingDefaults();
        if (errors.Count > 0)
        {
            return this.BadRequest(errors);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        this._logger.LogInformation("Added the missing default releases.");
        return this.Ok(service.Configuration.Releases.Select(ReleaseView.From));
    }

    private static async ValueTask<GameConfiguration> GetGameConfigurationAsync(IContext context, CancellationToken cancellationToken)
        => (await context.GetAsync<GameConfiguration>(cancellationToken).ConfigureAwait(false)).First();

    private async Task<ActionResult> ChangeAsync(
        string id,
        bool mustExist,
        Func<ContentReleaseService, IReadOnlyList<ReleaseValidationError>> change,
        string changeName,
        Func<ReleaseView?, ActionResult> createResult,
        CancellationToken cancellationToken)
    {
        using var context = this._persistenceContextProvider.CreateNewContext();
        var service = new ContentReleaseService(context, await GetGameConfigurationAsync(context, cancellationToken).ConfigureAwait(false));
        if (mustExist && service.Configuration.FindRelease(id) is null)
        {
            return this.NotFound();
        }

        var errors = change(service);
        if (errors.Count > 0)
        {
            return this.BadRequest(errors);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        this._logger.LogInformation("The release {release} was {change}.", id, changeName);
        return createResult(service.Configuration.FindRelease(id) is { } release ? ReleaseView.From(release) : null);
    }
}
