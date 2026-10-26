// <copyright file="ReleaseValidationErrorCode.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// The codes of the <see cref="ReleaseValidationError"/>s.
/// </summary>
public enum ReleaseValidationErrorCode
{
    /// <summary>
    /// The id isn't a lower case slug like <c>duel-arena</c>.
    /// </summary>
    InvalidId,

    /// <summary>
    /// Another release has the same id.
    /// </summary>
    DuplicateId,

    /// <summary>
    /// The name is empty.
    /// </summary>
    MissingName,

    /// <summary>
    /// A required release doesn't exist. The detail is its id.
    /// </summary>
    UnknownRequirement,

    /// <summary>
    /// The release requires itself, directly or through other releases.
    /// </summary>
    CyclicRequirement,

    /// <summary>
    /// The release is released or gets released now, but a required release isn't released. The detail is its id.
    /// </summary>
    RequirementNotReleased,

    /// <summary>
    /// The release is scheduled, but a required release isn't released or scheduled before it. The detail is its id.
    /// </summary>
    RequirementNotScheduledBefore,

    /// <summary>
    /// The content is part of another release, too. The detail describes the content and names the other release.
    /// </summary>
    OverlappingContent,

    /// <summary>
    /// The content doesn't exist in the game configuration, or an item range is empty. The detail describes the content.
    /// </summary>
    UnknownContent,

    /// <summary>
    /// The release was already released, so its content, requirements and schedule can't change anymore and it can't be deleted.
    /// </summary>
    AlreadyReleased,

    /// <summary>
    /// The release isn't released, so it can't be reverted.
    /// </summary>
    NotReleased,

    /// <summary>
    /// The scheduled point in time isn't in the future.
    /// </summary>
    ScheduledInThePast,
}
