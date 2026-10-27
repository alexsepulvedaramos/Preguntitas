using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Services;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Daily;

// "¿Quién la eligió?" and "¿Quién la lanzó?" (§17.17): one guess per question — the author when
// member-written, otherwise whoever actively chose it; a flash's launcher stays hidden until you
// guess, unless it is anonymous. A right guess always pays, past the daily cap.
[Collection(PostgresCollection.Name)]
public class GuessTheChooserTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    // Today's question, chosen by `selector` the way `kind` says, with the clock an hour after T.
    private async Task<SeededGroup> OpenAsync(
        SelectionKind kind,
        string selector = "bea",
        string? author = null,
        params string[] members
    )
    {
        var group = await SeedGroupAsync(members.Length > 0 ? members : ["ana", "bea", "carlos", "dani"]);
        var questionId = await AddQuestionAsync(
            group.Id,
            creatorId: author == null ? null : group[author],
            configure: q => q.Source = author == null ? QuestionSource.Pack : QuestionSource.UserCreated
        );
        await AddEntryAsync(group.Id, Today, questionId, group[selector], activated: true, selectionKind: kind);
        Clock.SetLocal(Today, DailyTime.AddHours(1));
        return group;
    }

    private async Task<VoteResponseDto> VoteAsync(SeededGroup group, string voter)
    {
        await using var db = NewContext();
        var target = group.UserIds.Values.First(id => id != group[voter]);
        var (result, response) = await NewDailyService(db)
            .VoteAsync(group.Id, group[voter], new CreateVoteDto { SelectedTargetUserId = target });
        result.Should().Be(VoteResult.Success);
        return response!;
    }

    private async Task<(GuessResult, GuessAuthorResponseDto?)> GuessAsync(SeededGroup group, string guesser, string named)
    {
        await using var db = NewContext();
        return await NewAuthorGuessService(db)
            .GuessAsync(group.Id, group[guesser], new GuessAuthorRequestDto { CreatorId = group[named] });
    }

    private async Task<DailyStatusDto> StatusAsync(SeededGroup group, string viewer)
    {
        await using var db = NewContext();
        return await NewDailyService(db).GetCurrentStatusAsync(group.Id, group[viewer]);
    }

    // ── The daily question ───────────────────────────────────────────────────

    [Theory]
    [InlineData(SelectionKind.Picked)]
    [InlineData(SelectionKind.Confirmed)]
    public async Task APackQuestionSomeoneChose_IsGuessedAsTheChooser(SelectionKind kind)
    {
        var group = await OpenAsync(kind);

        var vote = await VoteAsync(group, "ana");
        var (_, response) = await GuessAsync(group, "ana", "bea");

        vote.Guess!.Kind.Should().Be("chooser");
        vote.Guess.State.Should().Be(AuthorGuessDto.Pending);
        vote.Guess.CandidateIds.Should().BeEquivalentTo([group["bea"], group["carlos"], group["dani"]]);
        response!.Guess.Correct.Should().BeTrue();
        response.Guess.Author!.Username.Should().Be("bea");
        response.Coins.Earned.Should().Be(CoinRewards.GuessCorrect);
    }

    [Fact]
    public async Task TheChooser_DoesNotGuess_AndSeesHowManyCaughtThem()
    {
        var group = await OpenAsync(SelectionKind.Picked);
        await VoteAsync(group, "ana");
        await GuessAsync(group, "ana", "bea");
        await VoteAsync(group, "carlos");
        await GuessAsync(group, "carlos", "dani");

        var chooserVote = await VoteAsync(group, "bea");
        await using var db = NewContext();
        var (result, _) = await NewAuthorGuessService(db)
            .GuessAsync(group.Id, group["bea"], new GuessAuthorRequestDto { CreatorId = group["ana"] });

        chooserVote.Guess!.State.Should().Be(AuthorGuessDto.Mine);
        chooserVote.Guess.Kind.Should().Be("chooser");
        chooserVote.Guess.CaughtCount.Should().Be(1);
        chooserVote.Guess.GuessCount.Should().Be(2);
        result.Should().Be(GuessResult.NotGuessable);
    }

    [Fact]
    public async Task AnAutomaticPickNobodyTouched_HasNothingToGuess_AndWasDecidedByChance()
    {
        var group = await OpenAsync(SelectionKind.Auto);

        var vote = await VoteAsync(group, "ana");
        var status = await StatusAsync(group, "ana");

        vote.Guess.Should().BeNull();
        status.Today.Guess.Should().BeNull();
        status.Today.DecidedByChance.Should().BeTrue();
    }

    [Fact]
    public async Task PickingAnotherMembersQuestion_StillGuessesWhoWroteIt()
    {
        var group = await OpenAsync(SelectionKind.Picked, selector: "bea", author: "carlos");

        var vote = await VoteAsync(group, "ana");
        var (_, response) = await GuessAsync(group, "ana", "carlos");
        var status = await StatusAsync(group, "ana");

        vote.Guess!.Kind.Should().Be("author");
        response!.Guess.Correct.Should().BeTrue("the author is the answer, not the selector who picked it");
        status.Today.DecidedByChance.Should().BeFalse();
    }

    [Fact]
    public async Task AnEditedPackQuestion_IsStillGuessedAsTheChooser()
    {
        var group = await OpenAsync(SelectionKind.Picked);
        await using (var db = NewContext())
            await db.Questions.ExecuteUpdateAsync(s => s.SetProperty(q => q.CreatorId, group["bea"]));

        var vote = await VoteAsync(group, "ana");

        vote.Guess!.Kind.Should().Be("chooser", "pack clones never count as member-written, even edited (§17.3)");
    }

    [Fact]
    public async Task WithTwoMembers_ThereIsNoGuess()
    {
        var group = await OpenAsync(SelectionKind.Picked, "bea", null, "ana", "bea");

        var vote = await VoteAsync(group, "ana");

        vote.Guess.Should().BeNull();
    }

    [Fact]
    public async Task ARightGuess_PaysEvenWithTheDailyCapReached()
    {
        var group = await OpenAsync(SelectionKind.Picked);
        await VoteAsync(group, "ana");
        await using (var db = NewContext())
            await NewCoinService(db).GrantAsync(group["ana"], CoinReason.Vote, CoinRewards.DailyCap, group.Id, 999);

        var (_, response) = await GuessAsync(group, "ana", "bea");

        response!.Coins.Earned.Should().Be(CoinRewards.GuessCorrect);
        response.Coins.CapReached.Should().BeFalse();
    }

    // ── Flash: «¿Quién la lanzó?» ────────────────────────────────────────────

    private async Task<(SeededGroup Group, int FlashId)> LaunchAsync(FlashOptions? options = null, params string[] members)
    {
        var group = await SeedGroupAsync(members.Length > 0 ? members : ["ana", "bea", "carlos", "dani"]);
        await AddActivatedDayAsync(group.Id, Today.AddDays(-1), group[members.Length > 0 ? members[^1] : "dani"]);
        Clock.SetLocal(Today, new TimeOnly(10, 0));
        await using var db = NewContext();
        await db.Users.ExecuteUpdateAsync(s => s.SetProperty(u => u.Coins, 600));
        var (result, response) = await NewFlashService(db)
            .LaunchAsync(
                group.Id,
                group["ana"],
                new CreateQuestionDto { Text = "¿Quién se comería la última croqueta?", Type = QuestionType.Superlative },
                options: options
            );
        result.Should().Be(FlashResult.Success);
        return (group, response!.Flash.Id);
    }

    private async Task<FlashStateDto> FlashStateAsync(SeededGroup group, string viewer, int flashId)
    {
        await using var db = NewContext();
        return await NewFlashService(db).GetStateAsync(flashId, group[viewer]);
    }

    private async Task VoteFlashAsync(SeededGroup group, string voter, int flashId)
    {
        await using var db = NewContext();
        var target = group.UserIds.Values.First(id => id != group[voter]);
        var (result, _) = await NewFlashService(db)
            .VoteAsync(group.Id, group[voter], flashId, new CreateVoteDto { SelectedTargetUserId = target });
        result.Should().Be(FlashResult.Success);
    }

    private async Task<(GuessResult, GuessAuthorResponseDto?)> GuessFlashAsync(
        SeededGroup group,
        string guesser,
        int flashId,
        string? named,
        bool skip = false
    )
    {
        await using var db = NewContext();
        return await NewAuthorGuessService(db)
            .GuessFlashAsync(
                group.Id,
                group[guesser],
                flashId,
                new GuessAuthorRequestDto { CreatorId = named == null ? null : group[named], Skip = skip }
            );
    }

    [Fact]
    public async Task TheLauncher_IsHiddenFromOthers_UntilTheyGuess()
    {
        var (group, flashId) = await LaunchAsync();

        var before = await FlashStateAsync(group, "bea", flashId);
        await VoteFlashAsync(group, "bea", flashId);
        var voted = await FlashStateAsync(group, "bea", flashId);
        await using (var db = NewContext())
            (await NewFlashService(db).GetDetailAsync(group.Id, group["bea"], flashId))!.Author.Should().BeNull();
        var (_, response) = await GuessFlashAsync(group, "bea", flashId, "ana");
        var after = await FlashStateAsync(group, "bea", flashId);

        before.LaunchedBy.Should().BeNull();
        before.LauncherHidden.Should().BeTrue();
        before.Guess.Should().BeNull("you guess after voting");
        voted.Guess!.Kind.Should().Be("launcher");
        voted.Guess.State.Should().Be(AuthorGuessDto.Pending);
        voted.LaunchedBy.Should().BeNull();
        response!.Guess.Correct.Should().BeTrue();
        response.Coins.Earned.Should().Be(CoinRewards.GuessCorrect);
        after.LaunchedBy!.Username.Should().Be("ana");
        after.LauncherHidden.Should().BeFalse();

        await using var check = NewContext();
        (await check.CoinTransactions.AnyAsync(t =>
            t.UserId == group["bea"] && t.Reason == CoinReason.FlashGuessCorrect && t.RefId == flashId
        )).Should().BeTrue();
    }

    [Fact]
    public async Task SkippingAFlashGuess_RevealsTheLauncher()
    {
        var (group, flashId) = await LaunchAsync();
        await VoteFlashAsync(group, "bea", flashId);

        var (_, response) = await GuessFlashAsync(group, "bea", flashId, null, skip: true);
        var after = await FlashStateAsync(group, "bea", flashId);

        response!.Guess.State.Should().Be(AuthorGuessDto.Skipped);
        after.LaunchedBy!.Username.Should().Be("ana");
    }

    [Fact]
    public async Task TheLauncher_SeesHowManyCaughtThem()
    {
        var (group, flashId) = await LaunchAsync();
        await VoteFlashAsync(group, "bea", flashId);
        await GuessFlashAsync(group, "bea", flashId, "ana");

        var mine = await FlashStateAsync(group, "ana", flashId);

        mine.LaunchedBy!.Username.Should().Be("ana");
        mine.Guess!.State.Should().Be(AuthorGuessDto.Mine);
        mine.Guess.CaughtCount.Should().Be(1);
        mine.Guess.GuessCount.Should().Be(1);
    }

    [Fact]
    public async Task AnAnonymousFlash_HasNoGuess_AndNeverNamesItsLauncher()
    {
        var (group, flashId) = await LaunchAsync(new FlashOptions(AnonymousAuthor: true));
        await VoteFlashAsync(group, "bea", flashId);

        var state = await FlashStateAsync(group, "bea", flashId);
        var (result, _) = await GuessFlashAsync(group, "bea", flashId, "ana");

        state.Guess.Should().BeNull();
        state.LauncherHidden.Should().BeFalse();
        state.LaunchedBy.Should().BeNull();
        result.Should().Be(GuessResult.NotGuessable);
    }

    [Fact]
    public async Task InATwoMemberGroup_TheLauncherShowsAsBefore()
    {
        var (group, flashId) = await LaunchAsync(null, "ana", "bea");

        var state = await FlashStateAsync(group, "bea", flashId);

        state.LaunchedBy!.Username.Should().Be("ana");
        state.LauncherHidden.Should().BeFalse();
    }

    [Fact]
    public async Task TheHint_WorksOnAFlashToo()
    {
        var (group, flashId) = await LaunchAsync();
        await VoteFlashAsync(group, "bea", flashId);

        await using var db = NewContext();
        var (result, response) = await NewAuthorGuessService(db).BuyFlashHintAsync(group.Id, group["bea"], flashId);

        result.Should().Be(GuessResult.Success);
        response!.Guess.HintUsed.Should().BeTrue();
        response.Guess.CandidateIds.Should().HaveCount(2).And.Contain(group["ana"]);
        (await db.CoinTransactions.AnyAsync(t =>
            t.UserId == group["bea"] && t.Reason == CoinReason.FlashHint && t.Amount == -CoinRewards.AuthorHintPrice
        )).Should().BeTrue();
    }

    [Fact]
    public async Task TheMemberCard_CountsFlashGuessesToo()
    {
        var (group, flashId) = await LaunchAsync();
        await VoteFlashAsync(group, "bea", flashId);
        await GuessFlashAsync(group, "bea", flashId, "ana");

        await using var db = NewContext();
        var stats = await NewStreakService(db).GetMemberStatsAsync(group.Id, group["bea"]);

        stats!.GuessesMade.Should().Be(1);
        stats.GuessesCorrect.Should().Be(1);
    }
}
