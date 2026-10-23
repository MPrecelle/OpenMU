// <copyright file="ContentStage.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// The stages in which the content of the game gets unlocked, in chronological order.
/// </summary>
public enum ContentStage
{
    /// <summary>
    /// The classic content up to season 3 episode 1, without the third character evolution.
    /// </summary>
    Classic,

    /// <summary>
    /// Season 3 episode 2: third character evolution, master level, third wings and the Balgass maps.
    /// </summary>
    Season3,

    /// <summary>
    /// Season 4: socket items, Raklion, Vulcanus and Santa Village.
    /// </summary>
    Season4,

    /// <summary>
    /// Season 5: gens, cash shop, duel arena, Doppelganger, Imperial Guardian and Loren Market.
    /// </summary>
    Season5,

    /// <summary>
    /// Season 6: Rage Fighter and Karutan. Everything is unlocked.
    /// </summary>
    Season6,
}
