// <copyright file="CountdownWarning.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// A warning for the players that releases come, and that the server restarts then.
/// </summary>
/// <param name="Minutes">The minutes until the releases, rounded up.</param>
/// <param name="Releases">The releases.</param>
public sealed record CountdownWarning(int Minutes, IReadOnlyList<ContentRelease> Releases);
