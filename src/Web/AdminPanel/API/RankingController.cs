// <copyright file="RankingController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.ComponentModel.DataAnnotations;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Web.AdminPanel.Auth;

/// <summary>
/// API controller which reports the best characters, e.g. for the rankings of a website.
/// </summary>
[ApiController]
[Route("api/rankings")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.ApiSchemes, Policy = AdminPolicies.Viewer)]
public class RankingController : ControllerBase
{
    /// <summary>
    /// The maximum number of characters of a ranking.
    /// </summary>
    internal const int MaximumCount = 100;

    private readonly IPersistenceContextProvider _persistenceContextProvider;
    private readonly IDataSource<GameConfiguration> _gameConfigurationSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="RankingController"/> class.
    /// </summary>
    /// <param name="persistenceContextProvider">The persistence context provider.</param>
    /// <param name="gameConfigurationSource">The source of the game configuration.</param>
    public RankingController(IPersistenceContextProvider persistenceContextProvider, IDataSource<GameConfiguration> gameConfigurationSource)
    {
        this._persistenceContextProvider = persistenceContextProvider;
        this._gameConfigurationSource = gameConfigurationSource;
    }

    /// <summary>
    /// Gets the best characters, ordered by resets, master level, level and experience.
    /// </summary>
    /// <param name="count">The maximum number of characters, up to <see cref="MaximumCount"/>.</param>
    /// <param name="characterClass">The number of the character class to which the ranking is limited; all classes when omitted.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// <c>200</c> with the characters, best first, and <c>400</c> when the count is out of range or
    /// the character class doesn't exist. Characters of bot accounts, banned characters and game
    /// masters are not included.
    /// </returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RankingEntry>>> GetRankingAsync(
        [FromQuery, Range(1, MaximumCount)] int count = 50,
        [FromQuery(Name = "class")] byte? characterClass = null,
        CancellationToken cancellationToken = default)
    {
        var gameConfiguration = await this._gameConfigurationSource.GetOwnerAsync(Guid.Empty, cancellationToken).ConfigureAwait(false);
        var classes = gameConfiguration.CharacterClasses.ToDictionary(c => c.GetId());
        Guid? characterClassId = null;
        if (characterClass is { } number)
        {
            if (classes.Values.FirstOrDefault(c => c.Number == number) is not { } requestedClass)
            {
                return this.BadRequest();
            }

            characterClassId = requestedClass.GetId();
        }

        using var context = this._persistenceContextProvider.CreateNewPlayerContext(gameConfiguration);
        var query = new CharacterRankingQuery(Stats.Level, Stats.MasterLevel, Stats.Resets, count, characterClassId);
        var ranking = await context.GetCharacterRankingAsync(query, cancellationToken).ConfigureAwait(false);
        return ranking
            .Select(entry =>
            {
                var entryClass = entry.CharacterClassId is { } id ? classes.GetValueOrDefault(id) : null;
                return new RankingEntry(
                    entry.Name,
                    entryClass?.Number ?? 0,
                    entryClass?.Name.ValueInNeutralLanguage ?? string.Empty,
                    entry.Level,
                    entry.MasterLevel,
                    entry.Resets);
            })
            .ToList();
    }
}
