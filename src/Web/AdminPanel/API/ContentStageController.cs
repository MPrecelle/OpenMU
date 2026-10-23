// <copyright file="ContentStageController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Torvik.ContentStages;
using MUnique.OpenMU.Web.AdminPanel.Auth;

/// <summary>
/// Torvik MU: the API to show and change the <see cref="ContentStage"/> of the server, which locks the content of later seasons.
/// </summary>
[ApiController]
[Route("api/content-stage")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.ApiSchemes, Policy = AdminPolicies.Operator)]
public class ContentStageController : ControllerBase
{
    private readonly IPersistenceContextProvider _persistenceContextProvider;
    private readonly ILogger<ContentStageController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentStageController"/> class.
    /// </summary>
    /// <param name="persistenceContextProvider">The persistence context provider.</param>
    /// <param name="logger">The logger.</param>
    public ContentStageController(IPersistenceContextProvider persistenceContextProvider, ILogger<ContentStageController> logger)
    {
        this._persistenceContextProvider = persistenceContextProvider;
        this._logger = logger;
    }

    /// <summary>
    /// Gets the current stage, as stored in the database.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current stage.</returns>
    [HttpGet]
    public async Task<ActionResult<ContentStageState>> GetAsync(CancellationToken cancellationToken)
    {
        using var context = this._persistenceContextProvider.CreateNewContext();
        var gameConfiguration = (await context.GetAsync<GameConfiguration>(cancellationToken).ConfigureAwait(false)).First();
        return new ContentStageState(ContentStageApplier.GetConfiguration(gameConfiguration).CurrentStage);
    }

    /// <summary>
    /// Brings the server to the stage: the content of later stages gets locked, the other content unlocked.
    /// The game servers apply the changed data after "Reload configuration and restart" in the admin panel.
    /// </summary>
    /// <param name="stage">The stage, e.g. <c>Classic</c> or <c>Season6</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new stage; 400, if the stage is unknown.</returns>
    [HttpPost("{stage}")]
    public async Task<ActionResult<ContentStageState>> ApplyAsync(ContentStage stage, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(stage))
        {
            return this.BadRequest();
        }

        using var context = this._persistenceContextProvider.CreateNewContext();
        var gameConfiguration = (await context.GetAsync<GameConfiguration>(cancellationToken).ConfigureAwait(false)).First();
        var configuration = new ContentStageApplier(context).Apply(gameConfiguration, stage);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        this._logger.LogInformation("Applied the content stage {stage}.", stage);
        return new ContentStageState(configuration.CurrentStage);
    }

    /// <summary>
    /// The content stage of the server.
    /// </summary>
    /// <param name="Stage">The current stage.</param>
    public record ContentStageState([property: JsonConverter(typeof(JsonStringEnumConverter))] ContentStage Stage);
}
