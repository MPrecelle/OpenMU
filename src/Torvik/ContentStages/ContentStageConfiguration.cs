// <copyright file="ContentStageConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

/// <summary>
/// The configuration of the <see cref="ContentStagesPlugIn"/>.
/// </summary>
public class ContentStageConfiguration
{
    /// <summary>
    /// Gets or sets the current stage. Content of later stages is locked.
    /// </summary>
    /// <remarks>
    /// Change it through the <see cref="ContentStageApplier"/>, which also changes the configuration data accordingly.
    /// </remarks>
    public ContentStage CurrentStage { get; set; } = ContentStage.Season6;

    /// <summary>
    /// Gets or sets the original values of the configuration data which is changed when content gets locked.
    /// It's <c>null</c> when nothing is locked.
    /// </summary>
    public ContentStageSnapshot? Snapshot { get; set; }
}
