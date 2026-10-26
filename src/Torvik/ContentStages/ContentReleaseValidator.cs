// <copyright file="ContentReleaseValidator.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Torvik.ContentStages;

using System.Text.RegularExpressions;

/// <summary>
/// Validates the releases as a whole: their ids, requirements and content.
/// </summary>
public static partial class ContentReleaseValidator
{
    /// <summary>
    /// Validates the releases.
    /// </summary>
    /// <param name="releases">The releases.</param>
    /// <param name="gameConfiguration">The game configuration, which has to contain the content of the releases.</param>
    /// <returns>The errors; empty, if the releases are valid.</returns>
    public static List<ReleaseValidationError> Validate(IReadOnlyList<ContentRelease> releases, GameConfiguration gameConfiguration)
    {
        var errors = new List<ReleaseValidationError>();
        var releasesById = new Dictionary<string, ContentRelease>();
        foreach (var release in releases)
        {
            if (!IdPattern().IsMatch(release.Id))
            {
                errors.Add(new(ReleaseValidationErrorCode.InvalidId, release.Id));
            }
            else if (!releasesById.TryAdd(release.Id, release))
            {
                errors.Add(new(ReleaseValidationErrorCode.DuplicateId, release.Id));
            }

            if (string.IsNullOrWhiteSpace(release.Name))
            {
                errors.Add(new(ReleaseValidationErrorCode.MissingName, release.Id));
            }
        }

        foreach (var release in releases)
        {
            ValidateRequirements(release, releasesById, errors);
        }

        new ContentValidation(gameConfiguration, errors).Validate(releases);
        return errors;
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex IdPattern();

    private static void ValidateRequirements(ContentRelease release, Dictionary<string, ContentRelease> releasesById, List<ReleaseValidationError> errors)
    {
        foreach (var requiredId in release.Requires.Distinct())
        {
            if (!releasesById.TryGetValue(requiredId, out var required))
            {
                errors.Add(new(ReleaseValidationErrorCode.UnknownRequirement, release.Id, requiredId));
            }
            else if (release.IsReleased && !required.IsReleased)
            {
                errors.Add(new(ReleaseValidationErrorCode.RequirementNotReleased, release.Id, requiredId));
            }
            else if (!release.IsReleased && release.ScheduledAt is { } scheduledAt && !required.IsReleased && !(required.ScheduledAt <= scheduledAt))
            {
                errors.Add(new(ReleaseValidationErrorCode.RequirementNotScheduledBefore, release.Id, requiredId));
            }
        }

        if (RequiresItself(release, releasesById))
        {
            errors.Add(new(ReleaseValidationErrorCode.CyclicRequirement, release.Id));
        }
    }

    private static bool RequiresItself(ContentRelease release, Dictionary<string, ContentRelease> releasesById)
    {
        var visited = new HashSet<string>();
        var pending = new Stack<string>(release.Requires);
        while (pending.TryPop(out var id))
        {
            if (id == release.Id)
            {
                return true;
            }

            if (visited.Add(id) && releasesById.TryGetValue(id, out var required))
            {
                required.Requires.ForEach(pending.Push);
            }
        }

        return false;
    }

    /// <summary>
    /// Checks that the content exists and that no content is part of two releases.
    /// </summary>
    private sealed class ContentValidation
    {
        private readonly GameConfiguration _gameConfiguration;
        private readonly List<ReleaseValidationError> _errors;
        private readonly Dictionary<string, string> _owners = new();
        private readonly List<(ReleaseContent.ItemRange Reference, string ReleaseId)> _items = new();
        private readonly List<(ReleaseContent.QuestReference Reference, string ReleaseId)> _quests = new();
        private readonly List<(ReleaseContent.MiniGameReference Reference, string ReleaseId)> _miniGames = new();

        public ContentValidation(GameConfiguration gameConfiguration, List<ReleaseValidationError> errors)
        {
            this._gameConfiguration = gameConfiguration;
            this._errors = errors;
        }

        public void Validate(IEnumerable<ContentRelease> releases)
        {
            var quests = this._gameConfiguration.Monsters.SelectMany(m => m.Quests).ToList();
            foreach (var release in releases)
            {
                var content = release.Content;
                content.Maps.ForEach(map => this.ValidateNumbered(release, $"map {map}", this._gameConfiguration.Maps.Any(m => m.Number == map)));
                content.Npcs.ForEach(npc => this.ValidateNumbered(release, $"npc {npc}", this._gameConfiguration.Monsters.Any(m => m.Number == npc)));
                content.CharacterClasses.ForEach(number => this.ValidateNumbered(release, $"class {number}", this._gameConfiguration.CharacterClasses.Any(c => c.Number == number)));
                content.PlugIns.ForEach(plugIn => this.ValidateNumbered(release, $"plugin {plugIn}", this._gameConfiguration.PlugInConfigurations.Any(p => p.TypeId == plugIn)));

                foreach (var range in content.Items)
                {
                    var exists = range.FirstNumber <= range.LastNumber && this._gameConfiguration.Items.Any(range.Contains);
                    this.ValidateReference(release, range, exists, this._items, (a, b) => a.Overlaps(b));
                }

                foreach (var quest in content.Quests)
                {
                    this.ValidateReference(release, quest, quests.Any(quest.Matches), this._quests, (a, b) => a.Overlaps(b));
                }

                foreach (var miniGame in content.MiniGames)
                {
                    this.ValidateReference(release, miniGame, this._gameConfiguration.MiniGameDefinitions.Any(miniGame.Matches), this._miniGames, (a, b) => a.Overlaps(b));
                }
            }
        }

        private void ValidateNumbered(ContentRelease release, string description, bool exists)
        {
            if (!exists)
            {
                this._errors.Add(new(ReleaseValidationErrorCode.UnknownContent, release.Id, description));
            }

            if (!this._owners.TryAdd(description, release.Id) && this._owners[description] != release.Id)
            {
                this._errors.Add(new(ReleaseValidationErrorCode.OverlappingContent, release.Id, $"{description} ({this._owners[description]})"));
            }
        }

        private void ValidateReference<T>(ContentRelease release, T reference, bool exists, List<(T Reference, string ReleaseId)> seen, Func<T, T, bool> overlaps)
            where T : notnull
        {
            if (!exists)
            {
                this._errors.Add(new(ReleaseValidationErrorCode.UnknownContent, release.Id, reference.ToString()));
            }

            if (seen.FirstOrDefault(entry => entry.ReleaseId != release.Id && overlaps(entry.Reference, reference)) is { ReleaseId: { } otherId })
            {
                this._errors.Add(new(ReleaseValidationErrorCode.OverlappingContent, release.Id, $"{reference} ({otherId})"));
            }

            seen.Add((reference, release.Id));
        }
    }
}
