// <copyright file="ContentCatalogController.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Torvik.ContentStages;
using MUnique.OpenMU.Web.AdminPanel.Auth;

/// <summary>
/// Torvik MU: the <see cref="ContentCatalog"/>, which lists the content which can be part of a <see cref="ContentRelease"/>.
/// </summary>
[ApiController]
[Route("api/content-catalog")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.ApiSchemes, Policy = AdminPolicies.Operator)]
public class ContentCatalogController : ControllerBase
{
    private static readonly CultureInfo NameCulture = CultureInfo.GetCultureInfo("es");

    private readonly IPersistenceContextProvider _persistenceContextProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentCatalogController"/> class.
    /// </summary>
    /// <param name="persistenceContextProvider">The persistence context provider.</param>
    public ContentCatalogController(IPersistenceContextProvider persistenceContextProvider)
    {
        this._persistenceContextProvider = persistenceContextProvider;
    }

    /// <summary>
    /// Gets the catalog of the game configuration. The names are in spanish, when there is a translation.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The catalog.</returns>
    [HttpGet]
    public async Task<ActionResult<ContentCatalog>> GetAsync(CancellationToken cancellationToken)
    {
        using var context = this._persistenceContextProvider.CreateNewContext();
        var gameConfiguration = (await context.GetAsync<GameConfiguration>(cancellationToken).ConfigureAwait(false)).First();
        return Create(gameConfiguration);
    }

    /// <summary>
    /// Creates the catalog of a game configuration.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <returns>The catalog.</returns>
    internal static ContentCatalog Create(GameConfiguration gameConfiguration)
    {
        var plugInTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .SelectMany(PlugInManager.DiscoverPlugInTypes)
            .DistinctBy(type => type.GUID)
            .ToDictionary(type => type.GUID);

        return new ContentCatalog(
            gameConfiguration.Maps.OrderBy(m => m.Number).Select(m => new ContentCatalog.Map(m.Number, Translate(m.Name))).ToList(),
            gameConfiguration.Items.OrderBy(i => i.Group).ThenBy(i => i.Number).Select(i => new ContentCatalog.Item(i.Group, i.Number, Translate(i.Name))).ToList(),
            gameConfiguration.Monsters
                .Where(m => m.ObjectKind is NpcObjectKind.PassiveNpc or NpcObjectKind.Guard)
                .OrderBy(m => m.Number)
                .Select(m => new ContentCatalog.Npc(m.Number, Translate(m.Designation)))
                .ToList(),
            gameConfiguration.CharacterClasses.OrderBy(c => c.Number).Select(c => new ContentCatalog.CharacterClass(c.Number, Translate(c.Name))).ToList(),
            gameConfiguration.Monsters
                .SelectMany(m => m.Quests)
                .DistinctBy(q => (q.Group, q.Number))
                .OrderBy(q => q.Group).ThenBy(q => q.Number)
                .Select(q => new ContentCatalog.Quest(q.Group, q.Number, Translate(q.Name)))
                .ToList(),
            gameConfiguration.MiniGameDefinitions
                .DistinctBy(g => (g.Type, g.GameLevel))
                .OrderBy(g => g.Type).ThenBy(g => g.GameLevel)
                .Select(g => new ContentCatalog.MiniGame(g.Type, g.GameLevel, Translate(g.Name)))
                .ToList(),
            gameConfiguration.PlugInConfigurations
                .Select(p => plugInTypes.TryGetValue(p.TypeId, out var type) ? CreatePlugIn(type) : new ContentCatalog.PlugIn(p.TypeId, p.TypeId.ToString(), null))
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    private static string Translate(LocalizedString name) => name.GetTranslation(NameCulture) ?? string.Empty;

    private static ContentCatalog.PlugIn CreatePlugIn(Type type)
    {
        var display = type.GetCustomAttribute<DisplayAttribute>();
        return new ContentCatalog.PlugIn(type.GUID, display?.GetName() ?? type.Name, display?.GetDescription());
    }
}
