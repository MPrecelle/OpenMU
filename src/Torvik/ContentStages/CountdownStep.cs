// <copyright file="CountdownStep.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// The result of <see cref="ReleaseCountdown.Next"/>.
/// </summary>
/// <param name="Warnings">The warnings which have to be sent now.</param>
/// <param name="Due">The releases which have to be released now, in the order of their schedule.</param>
public sealed record CountdownStep(IReadOnlyList<CountdownWarning> Warnings, IReadOnlyList<ContentRelease> Due);
