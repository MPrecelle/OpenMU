// <copyright file="CharacterRankingQuery.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence;

using MUnique.OpenMU.AttributeSystem;

/// <summary>
/// Defines which characters a ranking contains, e.g. for the rankings of a website.
/// </summary>
/// <remarks>
/// The attributes are passed in, because they are defined by the game logic, which the persistence doesn't know.
/// </remarks>
/// <param name="LevelAttribute">The attribute which holds the level of a character.</param>
/// <param name="MasterLevelAttribute">The attribute which holds the master level of a character.</param>
/// <param name="ResetsAttribute">The attribute which holds the number of resets of a character.</param>
/// <param name="Count">The maximum number of characters.</param>
/// <param name="CharacterClassId">The identifier of the character class to which the ranking is limited, or <c>null</c> for all classes.</param>
public sealed record CharacterRankingQuery(
    AttributeDefinition LevelAttribute,
    AttributeDefinition MasterLevelAttribute,
    AttributeDefinition ResetsAttribute,
    int Count,
    Guid? CharacterClassId = null);
