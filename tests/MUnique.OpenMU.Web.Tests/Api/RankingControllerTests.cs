// <copyright file="RankingControllerTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Api;

using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.Web.AdminPanel.API;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>
/// Tests for the <see cref="RankingController"/>, which reports the best characters through the public API.
/// </summary>
[TestFixture]
public class RankingControllerTests
{
    private readonly CharacterClass _darkKnight = new BasicModel.CharacterClass { Id = Guid.NewGuid(), Number = 4, Name = "Dark Knight" };

    private readonly CharacterClass _darkWizard = new BasicModel.CharacterClass { Id = Guid.NewGuid(), Number = 0, Name = "Dark Wizard" };

    private InMemoryPersistenceContextProvider _persistenceContextProvider = null!;

    private RankingController _controller = null!;

    /// <summary>
    /// Sets up an empty in-memory database and the controller.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        this._persistenceContextProvider = new InMemoryPersistenceContextProvider();
        var gameConfiguration = new BasicModel.GameConfiguration();
        gameConfiguration.CharacterClasses.Add(this._darkKnight);
        gameConfiguration.CharacterClasses.Add(this._darkWizard);
        var gameConfigurationSource = new Mock<IDataSource<GameConfiguration>>();
        gameConfigurationSource
            .Setup(s => s.GetOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameConfiguration);
        this._controller = new RankingController(this._persistenceContextProvider, gameConfigurationSource.Object);
    }

    /// <summary>
    /// Resets count more than master levels, and master levels more than levels.
    /// </summary>
    [Test]
    public async Task RankingIsOrderedByResetsMasterLevelAndLevelAsync()
    {
        await this.AddAccountAsync(
            "account1",
            new CharacterData("HighLevel", this._darkKnight, Level: 400),
            new CharacterData("Master", this._darkKnight, Level: 400, MasterLevel: 10)).ConfigureAwait(false);
        await this.AddAccountAsync(
            "account2",
            new CharacterData("Resetter", this._darkWizard, Level: 10, Resets: 1),
            new CharacterData("LowLevel", this._darkWizard, Level: 5)).ConfigureAwait(false);

        var ranking = await this.GetRankingAsync().ConfigureAwait(false);

        Assert.That(ranking.Select(e => e.Name), Is.EqualTo(new[] { "Resetter", "Master", "HighLevel", "LowLevel" }));
        Assert.That(ranking[0], Is.EqualTo(new RankingEntry("Resetter", 0, "Dark Wizard", 10, 0, 1)));
    }

    /// <summary>
    /// The count limits the ranking to the best characters.
    /// </summary>
    [Test]
    public async Task RankingIsLimitedToCountAsync()
    {
        await this.AddAccountAsync(
            "account1",
            new CharacterData("First", this._darkKnight, Level: 3),
            new CharacterData("Second", this._darkKnight, Level: 2),
            new CharacterData("Third", this._darkKnight, Level: 1)).ConfigureAwait(false);

        var ranking = await this.GetRankingAsync(count: 2).ConfigureAwait(false);

        Assert.That(ranking.Select(e => e.Name), Is.EqualTo(new[] { "First", "Second" }));
    }

    /// <summary>
    /// The ranking can be limited to one character class.
    /// </summary>
    [Test]
    public async Task RankingCanBeLimitedToClassAsync()
    {
        await this.AddAccountAsync(
            "account1",
            new CharacterData("Knight", this._darkKnight, Level: 3),
            new CharacterData("Wizard", this._darkWizard, Level: 2)).ConfigureAwait(false);

        var ranking = await this.GetRankingAsync(characterClass: this._darkWizard.Number).ConfigureAwait(false);

        Assert.That(ranking.Select(e => e.Name), Is.EqualTo(new[] { "Wizard" }));
    }

    /// <summary>
    /// Characters of bot and template accounts, banned characters and game masters are not ranked.
    /// </summary>
    [Test]
    public async Task RankingExcludesBotsBannedCharactersAndGameMastersAsync()
    {
        var banned = new CharacterData("Banned", this._darkKnight, Level: 400, Status: CharacterStatus.Banned);
        var gameMaster = new CharacterData("GameMaster", this._darkKnight, Level: 400, Status: CharacterStatus.GameMaster);
        await this.AddAccountAsync("player", new CharacterData("Player", this._darkKnight, Level: 1), banned, gameMaster).ConfigureAwait(false);
        await this.AddAccountAsync("bot", a => a.IsBot = true, new CharacterData("Bot", this._darkKnight, Level: 400)).ConfigureAwait(false);
        await this.AddAccountAsync("template", a => a.IsTemplate = true, new CharacterData("Template", this._darkKnight, Level: 400)).ConfigureAwait(false);

        var ranking = await this.GetRankingAsync().ConfigureAwait(false);

        Assert.That(ranking.Select(e => e.Name), Is.EqualTo(new[] { "Player" }));
    }

    /// <summary>
    /// A character class which doesn't exist is answered with a bad request.
    /// </summary>
    [Test]
    public async Task RankingRejectsUnknownClassAsync()
    {
        var result = await this._controller.GetRankingAsync(50, 99, CancellationToken.None).ConfigureAwait(false);

        Assert.That(result.Result, Is.InstanceOf<BadRequestResult>());
    }

    private async ValueTask<IReadOnlyList<RankingEntry>> GetRankingAsync(int count = 50, byte? characterClass = null)
    {
        var result = await this._controller.GetRankingAsync(count, characterClass, CancellationToken.None).ConfigureAwait(false);
        return result.Value!;
    }

    private ValueTask AddAccountAsync(string loginName, params CharacterData[] characters)
    {
        return this.AddAccountAsync(loginName, _ => { }, characters);
    }

    private async ValueTask AddAccountAsync(string loginName, Action<Account> configure, params CharacterData[] characters)
    {
        using var context = this._persistenceContextProvider.CreateNewPlayerContext(new GameConfiguration());
        var account = context.CreateNew<Account>();
        account.LoginName = loginName;
        configure(account);
        foreach (var data in characters)
        {
            var character = context.CreateNew<Character>();
            character.Name = data.Name;
            character.CharacterClass = data.CharacterClass;
            character.CharacterStatus = data.Status;
            character.Attributes.Add(context.CreateNew<StatAttribute>(Stats.Level, (float)data.Level));
            character.Attributes.Add(context.CreateNew<StatAttribute>(Stats.MasterLevel, (float)data.MasterLevel));
            character.Attributes.Add(context.CreateNew<StatAttribute>(Stats.Resets, (float)data.Resets));
            account.Characters.Add(character);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    private sealed record CharacterData(string Name, CharacterClass CharacterClass, int Level, int MasterLevel = 0, int Resets = 0, CharacterStatus Status = CharacterStatus.Normal);
}
