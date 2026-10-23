// <copyright file="CharacterRankingEntry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence;

/// <summary>
/// A character of a ranking, see <see cref="CharacterRankingQuery"/>.
/// </summary>
/// <remarks>
/// The character class is given by its identifier, because it belongs to the game configuration.
/// </remarks>
/// <param name="Name">The name of the character.</param>
/// <param name="CharacterClassId">The identifier of the character class.</param>
/// <param name="Level">The level.</param>
/// <param name="MasterLevel">The master level.</param>
/// <param name="Resets">The number of resets.</param>
public sealed record CharacterRankingEntry(string Name, Guid? CharacterClassId, int Level, int MasterLevel, int Resets);
