// <copyright file="RankingEntry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.API;

/// <summary>
/// A character of the ranking returned by <see cref="RankingController.GetRankingAsync"/>.
/// </summary>
/// <param name="Name">The name of the character.</param>
/// <param name="CharacterClassNumber">The number of the character class, as the game client knows it.</param>
/// <param name="CharacterClassName">The name of the character class, in the neutral language.</param>
/// <param name="Level">The level.</param>
/// <param name="MasterLevel">The master level.</param>
/// <param name="Resets">The number of resets.</param>
public sealed record RankingEntry(string Name, byte CharacterClassNumber, string CharacterClassName, int Level, int MasterLevel, int Resets);
