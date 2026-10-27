// <copyright file="IShowLockedContentPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

using MUnique.OpenMU.GameLogic.Views;

/// <summary>
/// Interface of a view whose implementation informs the game client about the locked content, so it can hide its user interface.
/// </summary>
public interface IShowLockedContentPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the locked content.
    /// </summary>
    /// <param name="content">The locked content.</param>
    ValueTask ShowLockedContentAsync(LockedClientContent content);
}
