// <copyright file="ReleaseValidationError.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

using System.Text.Json.Serialization;

/// <summary>
/// An error which prevents a change of the <see cref="ContentStageConfiguration.Releases"/>.
/// </summary>
/// <remarks>
/// The clients (e.g. the web) show the error by its code, in their language.
/// </remarks>
/// <param name="Code">The code.</param>
/// <param name="ReleaseId">The id of the release which has the error. It isn't always the changed one, e.g. when a dependent release gets invalid.</param>
/// <param name="Detail">The detail, e.g. the overlapping content or the id of the other release.</param>
public record ReleaseValidationError(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ReleaseValidationErrorCode Code,
    string ReleaseId,
    string? Detail = null);
