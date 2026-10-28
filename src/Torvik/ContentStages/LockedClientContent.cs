// <copyright file="LockedClientContent.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// The locked content whose user interface the game client hides.
/// </summary>
/// <param name="IsGensLocked">If set to <c>true</c>, the gens are locked.</param>
/// <param name="IsCashShopLocked">If set to <c>true</c>, the cash shop is locked.</param>
/// <param name="LockedMaps">The numbers of the locked maps, ascending.</param>
public sealed record LockedClientContent(bool IsGensLocked, bool IsCashShopLocked, IReadOnlyList<short> LockedMaps);
