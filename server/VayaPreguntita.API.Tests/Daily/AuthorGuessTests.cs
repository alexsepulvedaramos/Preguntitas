using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Services;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Daily;

// "Guess the author" (§17.6) and its hint (§17.10): after voting on a member-written
// question, a member guesses who wrote it once, sees the answer at once and earns 5 coins
// when right.
[Collection(PostgresCollection.Name)]
public class AuthorGuessTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    // A question written by `author`, open today, with the clock an hour after T.
    private async Task<(SeededGroup Group, int EntryId)> OpenQuestionByAsync(
        string author,
        params string[] members
    )
    {
        var group = await SeedGroupAsync(members);
        var questionId = await AddQuestionAsync(group.Id, creatorId: group[author]);
        var entryId = await AddEntryAsync(group.Id, Today, questionId, group[members[0]], activated: true);
        Clock.SetLocal(Today, DailyTime.AddHours(1));
        return (group, entryId);
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

    private async Task<(GuessResult, GuessAuthorResponseDto?)> GuessAsync(
        SeededGroup group,
        string guesser,
        int? creatorId,
        bool skip = false
    )
    {
        await using var db = NewContext();
        return await NewAuthorGuessService(db)
            .GuessAsync(group.Id, group[guesser], new GuessAuthorRequestDto { CreatorId = creatorId, Skip = skip });
    }

    private async Task<(GuessResult, AuthorHintResponseDto?)> BuyHintAsync(SeededGroup group, string buyer)
    {
        await using var db = NewContext();
        return await NewAuthorGuessService(db).BuyHintAsync(group.Id, group[buyer]);
    }

    private async Task GiveCoinsAsync(int userId, int coins)
    {
        await using var db = NewContext();
        await db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.Coins, coins));
    }

    private async Task<int> BalanceAsync(int userId)
    {
        await using var db = NewContext();
        return await db.Users.Where(u => u.Id == userId).Select(u => u.Coins).SingleAsync();
    }

    [Fact]
    public async Task AfterVoting_TheGuessIsPending_WithEveryoneButYouAsCandidates()
    {
        var (group, _) = await OpenQuestionByAsync("bea", "ana", "bea", "carlos", "dani");

        var vote = await VoteAsync(group, "ana");

        vote.Guess!.State.Should().Be(AuthorGuessDto.Pending);
        vote.Guess.CandidateIds.Should().BeEquivalentTo([group["bea"], group["carlos"], group["dani"]]);
        vote.Guess.Author.Should().BeNull("the author is only revealed after guessing or skipping");
        vote.Guess.HintAvailable.Should().BeTrue();
        vote.Guess.HintPrice.Should().Be(CoinRewards.AuthorHintPrice);
    }

    [Fact]
    public async Task ARightGuess_RevealsTheAuthorAndPaysOnce()
    {
        var (group, _) = await OpenQuestionByAsync("bea", "ana", "bea", "carlos");
        await VoteAsync(group, "ana");
        var before = await BalanceAsync(group["ana"]);

        var (result, response) = await GuessAsync(group, "ana", group["bea"]);
        var (again, _) = await GuessAsync(group, "ana", group["carlos"]);

        result.Should().Be(GuessResult.Success);
        response!.Guess.State.Should().Be(AuthorGuessDto.Guessed);
        response.Guess.Correct.Should().BeTrue();
        response.Guess.Author!.Username.Should().Be("bea");
        response.Coins.Earned.Should().Be(CoinRewards.GuessCorrect);
        (await BalanceAsync(group["ana"])).Should().Be(before + CoinRewards.GuessCorrect);
        again.Should().Be(GuessResult.AlreadyGuessed);
    }

    [Fact]
    public async Task AWrongGuess_RevealsTheAuthorButPaysNothing()
    {
        var (group, _) = await OpenQuestionByAsync("bea", "ana", "bea", "carlos");
        await VoteAsync(group, "ana");

        var (_, response) = await GuessAsync(group, "ana", group["carlos"]);

        response!.Guess.Correct.Should().BeFalse();
        response.Guess.GuessedUserId.Should().Be(group["carlos"]);
        response.Guess.Author!.Username.Should().Be("bea");
        response.Coins.Earned.Should().Be(0);
    }

    [Fact]
    public async Task SkippingGivesUp_AndShowsTheAuthor()
    {
        var (group, _) = await OpenQuestionByAsync("bea", "ana", "bea", "carlos");
        await VoteAsync(group, "ana");

        var (result, response) = await GuessAsync(group, "ana", null, skip: true);
        var (later, _) = await GuessAsync(group, "ana", group["bea"]);

        result.Should().Be(GuessResult.Success);
        response!.Guess.State.Should().Be(AuthorGuessDto.Skipped);
        response.Guess.Author!.Username.Should().Be("bea");
        later.Should().Be(GuessResult.AlreadyGuessed);
    }

    [Fact]
    public async Task YouHaveToVoteBeforeGuessing()
    {
        var (group, _) = await OpenQuestionByAsync("bea", "ana", "bea", "carlos");

        var (result, _) = await GuessAsync(group, "ana", group["bea"]);

        result.Should().Be(GuessResult.MustVoteFirst);
    }

    [Fact]
    public async Task TheAuthorDoesntGuess_ButSeesHowManyCaughtThem()
    {
        var (group, _) = await OpenQuestionByAsync("bea", "ana", "bea", "carlos", "dani");
        await VoteAsync(group, "ana");
        await VoteAsync(group, "carlos");
        await VoteAsync(group, "dani");
        await GuessAsync(group, "ana", group["bea"]);
        await GuessAsync(group, "carlos", group["dani"]);
        await GuessAsync(group, "dani", null, skip: true);

        var authorVote = await VoteAsync(group, "bea");
        var (ownGuess, _) = await GuessAsync(group, "bea", group["ana"]);

        authorVote.Guess!.State.Should().Be(AuthorGuessDto.Mine);
        authorVote.Guess.CaughtCount.Should().Be(1);
        authorVote.Guess.GuessCount.Should().Be(2, "a skip isn't a guess");
        ownGuess.Should().Be(GuessResult.NotGuessable);
    }

    [Fact]
    public async Task PackQuestionsAndTwoMemberGroups_AreNotGuessable()
    {
        var pair = await SeedGroupAsync("ana", "bea");
        var pairQuestion = await AddQuestionAsync(pair.Id, creatorId: pair["bea"]);
        await AddEntryAsync(pair.Id, Today, pairQuestion, pair["ana"], activated: true);

        var trio = await SeedGroupAsync("carlos", "dani", "eva");
        var packClone = await AddQuestionAsync(
            trio.Id,
            creatorId: trio["dani"],
            configure: q => q.Source = QuestionSource.Pack
        );
        await AddEntryAsync(trio.Id, Today, packClone, trio["carlos"], activated: true);
        Clock.SetLocal(Today, DailyTime.AddHours(1));

        (await VoteAsync(pair, "ana")).Guess.Should().BeNull();
        (await VoteAsync(trio, "carlos")).Guess.Should().BeNull();
        (await GuessAsync(trio, "carlos", trio["dani"])).Item1.Should().Be(GuessResult.NotGuessable);
    }

    [Fact]
    public async Task TheHint_HidesHalfTheWrongCandidates_AndIsChargedOnce()
    {
        // 5 candidates for ana: bea (the author) + 4 wrong ones → the hint hides 2.
        var (group, _) = await OpenQuestionByAsync("bea", "ana", "bea", "carlos", "dani", "eva", "fran");
        await VoteAsync(group, "ana");
        await GiveCoinsAsync(group["ana"], 50);

        var (result, first) = await BuyHintAsync(group, "ana");
        var (_, second) = await BuyHintAsync(group, "ana");

        result.Should().Be(GuessResult.Success);
        first!.Guess.CandidateIds.Should().HaveCount(3).And.Contain(group["bea"]);
        first.Guess.HintUsed.Should().BeTrue();
        first.Guess.HintAvailable.Should().BeFalse();
        first.Balance.Should().Be(50 - CoinRewards.AuthorHintPrice);
        second!.Guess.CandidateIds.Should().Equal(first.Guess.CandidateIds, "the hint is fixed per question");
        second.Balance.Should().Be(first.Balance, "buying it again charges nothing");

        var hidden = group.UserIds.Values.Except(first.Guess.CandidateIds).Except([group["ana"]]).First();
        (await GuessAsync(group, "ana", hidden)).Item1.Should().Be(GuessResult.InvalidCandidate);
        (await GuessAsync(group, "ana", group["bea"])).Item2!.Coins.Earned.Should().Be(CoinRewards.GuessCorrect);
    }

    [Fact]
    public async Task TheHint_NeedsCoins_AndAtLeastOneWrongCandidateToHide()
    {
        var (big, _) = await OpenQuestionByAsync("bea", "ana", "bea", "carlos", "dani");
        await VoteAsync(big, "ana");
        await GiveCoinsAsync(big["ana"], CoinRewards.AuthorHintPrice - 1);

        var (broke, _) = await BuyHintAsync(big, "ana");

        broke.Should().Be(GuessResult.NotEnoughCoins);

        // In a group of 3 there is only one wrong candidate: half of it rounds down to none.
        var trio = await SeedGroupAsync("eva", "fran", "gus");
        var questionId = await AddQuestionAsync(trio.Id, creatorId: trio["fran"]);
        await AddEntryAsync(trio.Id, Today, questionId, trio["eva"], activated: true);
        var vote = await VoteAsync(trio, "eva");
        await GiveCoinsAsync(trio["eva"], 100);

        vote.Guess!.HintAvailable.Should().BeFalse();
        (await BuyHintAsync(trio, "eva")).Item1.Should().Be(GuessResult.HintNotAvailable);
    }

    [Fact]
    public async Task DailyCurrent_ShowsWhoVoted_AndKeepsTheGuessAcrossReloads()
    {
        var (group, _) = await OpenQuestionByAsync("bea", "ana", "bea", "carlos");
        await VoteAsync(group, "ana");
        await GuessAsync(group, "ana", group["carlos"]);

        await using var db = NewContext();
        var ana = await NewDailyService(db).GetCurrentStatusAsync(group.Id, group["ana"]);
        var carlos = await NewDailyService(db).GetCurrentStatusAsync(group.Id, group["carlos"]);

        ana.Today.VoterIds.Should().Equal(group["ana"]);
        ana.Today.MemberCount.Should().Be(3);
        ana.Today.Guess!.State.Should().Be(AuthorGuessDto.Guessed);
        ana.Today.Guess.Author!.Username.Should().Be("bea");
        carlos.Today.Status.Should().Be("voting");
        carlos.Today.Guess.Should().BeNull("nothing about the author before voting");
    }

    [Fact]
    public async Task History_ShowsTheAuthorOnlyOnceClosed_WithYourGuess()
    {
        var (group, _) = await OpenQuestionByAsync("bea", "ana", "bea", "carlos");
        await VoteAsync(group, "ana");
        await GuessAsync(group, "ana", group["bea"]);

        await using (var db = NewContext())
        {
            var whileOpen = await new QuestionsService(db, TestMapper.Instance).GetByDateAsync(group.Id, Today);
            whileOpen!.Author.Should().BeNull();
        }

        await AddActivatedDayAsync(group.Id, Today.AddDays(1), group["carlos"]);

        await using (var db = NewContext())
        {
            var service = new QuestionsService(db, TestMapper.Instance);
            var detail = await service.GetByDateAsync(group.Id, Today);
            var page = await service.GetHistoryAsync(group.Id, group["ana"], before: null);
            var carlosPage = await service.GetHistoryAsync(group.Id, group["carlos"], before: null);

            detail!.Author!.Username.Should().Be("bea");
            page.Items.Single().AuthorUsername.Should().Be("bea");
            page.Items.Single().MyGuessCorrect.Should().BeTrue();
            carlosPage.Items.Single().MyGuessCorrect.Should().BeNull();
        }
    }

    [Fact]
    public async Task ThePool_SaysWhichQuestionsAreMine_WithoutNamingCreators()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await AddQuestionAsync(group.Id, text: "De ana", creatorId: group["ana"]);
        await AddQuestionAsync(group.Id, text: "De bea", creatorId: group["bea"]);

        await using var db = NewContext();
        var page = await new QuestionsService(db, TestMapper.Instance).GetPoolAsync(group.Id, group["ana"]);

        page.Items.Single(q => q.Text == "De ana").IsMine.Should().BeTrue();
        page.Items.Single(q => q.Text == "De bea").IsMine.Should().BeFalse();
    }

    [Fact]
    public async Task TheMemberCard_CountsGuessesMadeAndRight()
    {
        var (group, _) = await OpenQuestionByAsync("bea", "ana", "bea", "carlos");
        await VoteAsync(group, "ana");
        await GuessAsync(group, "ana", group["bea"]);

        await using var db = NewContext();
        var stats = await NewStreakService(db).GetMemberStatsAsync(group.Id, group["ana"]);

        stats!.GuessesMade.Should().Be(1);
        stats.GuessesCorrect.Should().Be(1);
        stats.TimesSelector.Should().Be(0, "picking today's open question isn't shown yet: it would point at the author");
    }
}
