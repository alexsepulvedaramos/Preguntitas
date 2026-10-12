using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Services;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Daily;

// History gating + late vote (§17.7): a past day's results show only to whoever voted it,
// and a closed question can still be voted from the history — counted in the tally, but
// never for streak, coins or push.
[Collection(PostgresCollection.Name)]
public class LateVoteTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Yesterday = new(2026, 10, 4);
    private static readonly DateOnly Today = new(2026, 10, 5);

    // Yesterday's question (ana voted it) closed when today's opened; the clock sits after T.
    private async Task<SeededGroup> SeedClosedDayAsync()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        await AddActivatedDayAsync(group.Id, Yesterday, group["ana"]);
        Clock.SetLocal(Yesterday, DailyTime.AddHours(1));
        await VoteOnTimeAsync(group, "ana", "bea");
        await AddActivatedDayAsync(group.Id, Today, group["bea"]);
        Clock.SetLocal(Today, DailyTime.AddHours(1));
        return group;
    }

    private async Task VoteOnTimeAsync(SeededGroup group, string voter, string target)
    {
        await using var db = NewContext();
        var (result, _) = await NewDailyService(db)
            .VoteAsync(group.Id, group[voter], new() { SelectedTargetUserId = group[target] });
        result.Should().Be(VoteResult.Success);
    }

    private async Task<VoteResult> LateVoteAsync(SeededGroup group, string voter, DateOnly date, string target)
    {
        await using var db = NewContext();
        return await NewDailyService(db)
            .LateVoteAsync(group.Id, group[voter], date, new() { SelectedTargetUserId = group[target] });
    }

    private async Task<QuestionResultDto?> DetailAsync(SeededGroup group, string viewer, DateOnly date)
    {
        await using var db = NewContext();
        return await new QuestionsService(db, TestMapper.Instance).GetByDateAsync(group.Id, group[viewer], date);
    }

    [Fact]
    public async Task PastResults_AreLockedUntilYouVote()
    {
        var group = await SeedClosedDayAsync();

        var ana = await DetailAsync(group, "ana", Yesterday);
        var bea = await DetailAsync(group, "bea", Yesterday);

        ana!.Locked.Should().BeFalse();
        ana.Results.Should().ContainSingle();
        ana.LateVoteQuestion.Should().BeNull();
        bea!.Locked.Should().BeTrue();
        bea.Results.Should().BeEmpty();
        bea.TotalVotes.Should().Be(1, "the vote count stays visible as a teaser");
        bea.LateVoteQuestion.Should().NotBeNull();
    }

    [Fact]
    public async Task TheOpenQuestion_IsLockedWithoutALateVote_AndAPendingOneIsHidden()
    {
        var group = await SeedClosedDayAsync();
        var pendingQuestion = await AddQuestionAsync(group.Id, text: "Mañana");
        await AddEntryAsync(group.Id, Today.AddDays(1), pendingQuestion, group["carlos"], activated: false);

        var open = await DetailAsync(group, "carlos", Today);
        var pending = await DetailAsync(group, "carlos", Today.AddDays(1));

        open!.Locked.Should().BeTrue();
        open.LateVoteQuestion.Should().BeNull("the open question is voted from the group screen");
        pending.Should().BeNull();
        (await LateVoteAsync(group, "carlos", Today, "ana")).Should().Be(VoteResult.QuestionNotClosed);
        (await LateVoteAsync(group, "carlos", Today.AddDays(1), "ana")).Should().Be(VoteResult.QuestionNotFound);
    }

    [Fact]
    public async Task ALateVote_CountsInTheTally_ButEarnsNothing()
    {
        var group = await SeedClosedDayAsync();
        await using (var db = NewContext())
        {
            var bea = await db.GroupMembers.SingleAsync(m => m.GroupId == group.Id && m.UserId == group["bea"]);
            bea.CurrentStreak.Should().Be(0);
        }

        var result = await LateVoteAsync(group, "bea", Yesterday, "carlos");
        var again = await LateVoteAsync(group, "bea", Yesterday, "ana");

        result.Should().Be(VoteResult.Success);
        again.Should().Be(VoteResult.AlreadyVoted);

        var detail = await DetailAsync(group, "bea", Yesterday);
        detail!.Locked.Should().BeFalse();
        detail.TotalVotes.Should().Be(2);

        await using (var db = NewContext())
        {
            var vote = await db.Votes.SingleAsync(v => v.UserId == group["bea"]);
            vote.IsLate.Should().BeTrue();
            var bea = await db.GroupMembers.SingleAsync(m => m.GroupId == group.Id && m.UserId == group["bea"]);
            bea.CurrentStreak.Should().Be(0);
            (await db.CoinTransactions.AnyAsync(c => c.UserId == group["bea"])).Should().BeFalse();
            (await db.Users.SingleAsync(u => u.Id == group["bea"])).Coins.Should().Be(0);
        }
        Notifications.Verify(
            n => n.SendUserVotedAsync(group.Id, group["bea"], It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ALateVote_IsIgnoredWhenTheStreakIsRebuilt()
    {
        var group = await SeedClosedDayAsync();
        await LateVoteAsync(group, "bea", Yesterday, "carlos");

        await using (var db = NewContext())
            await NewStreakService(db).RecalculateMemberAsync(group.Id, group["bea"]);

        await using (var db = NewContext())
        {
            var bea = await db.GroupMembers.SingleAsync(m => m.GroupId == group.Id && m.UserId == group["bea"]);
            bea.CurrentStreak.Should().Be(0);
            bea.BestStreak.Should().Be(0);
        }
    }

    [Fact]
    public async Task ALateVote_IsValidatedLikeALiveOne()
    {
        var group = await SeedClosedDayAsync();
        var outsider = await SeedGroupAsync("zoe");

        var result = await LateVoteAsync(
            new SeededGroup(group.Id, new Dictionary<string, int>(group.UserIds) { ["zoe"] = outsider["zoe"] }),
            "bea",
            Yesterday,
            "zoe"
        );

        result.Should().Be(VoteResult.InvalidPayload);
    }
}
