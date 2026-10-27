using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Coins;

// Coin economy (rama 34, §17.2): rewards paid in the vote request, the first-voter bonus,
// streak tiers, the daily cap, idempotency and the ledger history.
[Collection(PostgresCollection.Name)]
public class CoinTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    private async Task<VoteResponseDto> VoteAsync(int groupId, int userId)
    {
        await using var db = NewContext();
        var targetId = await db
            .GroupMembers.Where(m => m.GroupId == groupId && m.UserId != userId)
            .Select(m => m.UserId)
            .FirstAsync();
        var (result, response) = await NewDailyService(db)
            .VoteAsync(groupId, userId, new() { SelectedTargetUserId = targetId });
        result.Should().Be(VoteResult.Success);
        return response!;
    }

    private async Task<int> BalanceAsync(int userId)
    {
        await using var db = NewContext();
        return await db.Users.Where(u => u.Id == userId).Select(u => u.Coins).SingleAsync();
    }

    [Fact]
    public async Task AVote_PaysFive_AndTheFirstVoterGetsThreeMore()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        await AddActivatedDayAsync(group.Id, Today, group["ana"]);
        Clock.SetLocal(Today, DailyTime.AddHours(1));

        var first = await VoteAsync(group.Id, group["bea"]);
        var second = await VoteAsync(group.Id, group["carlos"]);

        first.Coins.Earned.Should().Be(CoinRewards.Vote + CoinRewards.FirstVoter);
        first.Coins.Balance.Should().Be(8);
        second.Coins.Earned.Should().Be(CoinRewards.Vote);
        (await BalanceAsync(group["bea"])).Should().Be(8);
        (await BalanceAsync(group["carlos"])).Should().Be(5);

        await using var db = NewContext();
        (await db.CoinTransactions.CountAsync(t => t.Reason == CoinReason.FirstVoter)).Should().Be(1);
        (await db.DailyEntries.SingleAsync()).FirstVoterUserId.Should().Be(group["bea"]);
    }

    [Fact]
    public async Task TwoSimultaneousVotes_OnlyOneGetsTheFirstVoterBonus()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        await AddActivatedDayAsync(group.Id, Today, group["ana"]);
        Clock.SetLocal(Today, DailyTime.AddHours(1));

        var responses = await Task.WhenAll(VoteAsync(group.Id, group["bea"]), VoteAsync(group.Id, group["carlos"]));

        responses.Sum(r => r.Coins.Earned).Should().Be(2 * CoinRewards.Vote + CoinRewards.FirstVoter);
        await using var db = NewContext();
        (await db.CoinTransactions.CountAsync(t => t.Reason == CoinReason.FirstVoter)).Should().Be(1);
    }

    private async Task<List<VoteResponseDto>> VoteDaysAsync(SeededGroup group, string voter, int days)
    {
        var responses = new List<VoteResponseDto>();
        for (var day = 0; day < days; day++)
        {
            var date = Today.AddDays(day);
            await AddActivatedDayAsync(group.Id, date, group["ana"]);
            Clock.SetLocal(date, DailyTime.AddHours(1));
            responses.Add(await VoteAsync(group.Id, group[voter]));
        }
        return responses;
    }

    [Fact]
    public async Task ReachingAStreakTier_ForTheFirstTime_PaysTheBonusAndTheTierReward()
    {
        var group = await SeedGroupAsync("ana", "bea");

        var responses = await VoteDaysAsync(group, "bea", 3);

        responses[1].Coins.StreakBonus.Should().Be(0);
        responses[2].Streak.Current.Should().Be(3);
        responses[2].Coins.StreakBonus.Should().Be(1);
        responses[2].Coins.Earned.Should()
            .Be(CoinRewards.Vote + CoinRewards.FirstVoter + 1 + CoinRewards.StreakTiers[3]);
        (await BalanceAsync(group["bea"])).Should().Be(3 * 8 + 1 + CoinRewards.StreakTiers[3]);
    }

    [Fact]
    public async Task ReachingAStreakTier_Again_PaysOnlyTheDailyBonus()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await using (var db = NewContext())
            await db.Users.Where(u => u.Id == group["bea"])
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.HighestStreakEver, 10));

        var responses = await VoteDaysAsync(group, "bea", 3);

        responses[2].Coins.StreakBonus.Should().Be(1);
        responses[2].Coins.Earned.Should().Be(CoinRewards.Vote + CoinRewards.FirstVoter + 1);
        await using var check = NewContext();
        (await check.CoinTransactions.AnyAsync(t => t.Reason == CoinReason.StreakTier)).Should().BeFalse();
    }

    [Fact]
    public async Task TheDailyCap_StopsCappedRewardsAcrossGroups()
    {
        // 8 coins per group (vote + first voter): five groups fill the 40-coin cap exactly.
        var bea = 0;
        var groups = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            var group = await SeedGroupAsync($"ana{i}", $"bea{i}");
            await AddActivatedDayAsync(group.Id, Today, group[$"ana{i}"]);
            groups.Add(group.Id);
            if (i == 0)
                bea = group["bea0"];
            else
            {
                await using var db = NewContext();
                db.GroupMembers.Add(new() { GroupId = group.Id, UserId = bea, JoinedAt = GroupCreatedUtc });
                await db.SaveChangesAsync();
            }
        }
        Clock.SetLocal(Today, DailyTime.AddHours(1));

        var responses = new List<VoteResponseDto>();
        foreach (var groupId in groups)
            responses.Add(await VoteAsync(groupId, bea));

        responses.Take(5).Should().AllSatisfy(r => r.Coins.Earned.Should().Be(8));
        responses[5].Coins.Earned.Should().Be(0);
        responses[5].Coins.CapReached.Should().BeTrue();
        (await BalanceAsync(bea)).Should().Be(CoinRewards.DailyCap);

        // A new local day starts a fresh cap.
        await AddActivatedDayAsync(groups[0], Today.AddDays(1), bea);
        Clock.SetLocal(Today.AddDays(1), DailyTime.AddHours(1));
        (await VoteAsync(groups[0], bea)).Coins.Earned.Should().Be(8);
    }

    [Fact]
    public async Task UncappedRewards_ArePaidEvenWithTheCapReached()
    {
        var group = await SeedGroupAsync("ana");
        Clock.SetLocal(Today, DailyTime.AddHours(1));
        await using var db = NewContext();
        var coins = NewCoinService(db);

        await coins.GrantAsync(group["ana"], CoinReason.Vote, CoinRewards.DailyCap, group.Id, 1);
        var capped = await coins.GrantAsync(group["ana"], CoinReason.Vote, 5, group.Id, 2);
        var tier = await coins.GrantAsync(group["ana"], CoinReason.StreakTier, 20, group.Id, 2);

        capped.Should().Be(new Services.CoinGrant(0, true));
        tier.Should().Be(new Services.CoinGrant(20, false));
        (await BalanceAsync(group["ana"])).Should().Be(CoinRewards.DailyCap + 20);
    }

    [Theory]
    [InlineData(SelectionKind.Auto, 0)]
    [InlineData(SelectionKind.Confirmed, CoinRewards.SelectorChoice)]
    [InlineData(SelectionKind.Picked, CoinRewards.SelectorChoice)]
    [InlineData(SelectionKind.Created, CoinRewards.SelectorChoice + CoinRewards.SelectorCreatedBonus)]
    public async Task TheSelector_IsPaidAtActivation_OnlyForAnActiveChoice(SelectionKind kind, int expected)
    {
        var group = await SeedGroupAsync("ana", "bea");
        var questionId = await AddQuestionAsync(group.Id);
        await AddEntryAsync(group.Id, Today, questionId, group["bea"], activated: false, selectionKind: kind);

        Clock.SetLocal(Today, DailyTime.AddMinutes(-1));
        await TickAsync();
        (await BalanceAsync(group["bea"])).Should().Be(0);

        Clock.SetLocal(Today, DailyTime);
        await TickAsync();
        await TickAsync();
        (await BalanceAsync(group["bea"])).Should().Be(expected);
    }

    [Theory]
    [InlineData(SelectionKind.Auto)]
    [InlineData(SelectionKind.Picked)]
    public async Task TheAuthor_IsPaidWhenTheirQuestionIsPlayed(SelectionKind kind)
    {
        var group = await SeedGroupAsync("ana", "bea");
        var questionId = await AddQuestionAsync(group.Id, creatorId: group["ana"]);
        await AddEntryAsync(group.Id, Today, questionId, group["bea"], activated: false, selectionKind: kind);

        Clock.SetLocal(Today, DailyTime);
        await TickAsync();
        await TickAsync();

        (await BalanceAsync(group["ana"])).Should().Be(CoinRewards.QuestionPlayed);
    }

    [Theory]
    [InlineData(SelectionKind.Auto, CoinRewards.QuestionPlayed)]
    [InlineData(SelectionKind.Picked, CoinRewards.SelectorChoice)]
    [InlineData(SelectionKind.Created, CoinRewards.SelectorChoice + CoinRewards.SelectorCreatedBonus)]
    public async Task AnAuthorWhoChoseTheirOwnQuestion_GetsOnlyTheSelectorRewards(SelectionKind kind, int expected)
    {
        var group = await SeedGroupAsync("ana");
        var questionId = await AddQuestionAsync(group.Id, creatorId: group["ana"]);
        await AddEntryAsync(group.Id, Today, questionId, group["ana"], activated: false, selectionKind: kind);

        Clock.SetLocal(Today, DailyTime);
        await TickAsync();

        (await BalanceAsync(group["ana"])).Should().Be(expected);
    }

    [Fact]
    public async Task AnAuthorWhoLeftTheGroup_IsNotPaid()
    {
        var group = await SeedGroupAsync("ana", "bea");
        var questionId = await AddQuestionAsync(group.Id, creatorId: group["ana"]);
        await AddEntryAsync(group.Id, Today, questionId, group["bea"], activated: false);
        await using (var db = NewContext())
            await db.GroupMembers.Where(m => m.GroupId == group.Id && m.UserId == group["ana"]).ExecuteDeleteAsync();

        Clock.SetLocal(Today, DailyTime);
        await TickAsync();

        (await BalanceAsync(group["ana"])).Should().Be(0);
    }

    [Fact]
    public async Task ARewardIsPaidOncePerReference()
    {
        var group = await SeedGroupAsync("ana");
        await using var db = NewContext();
        var coins = NewCoinService(db);

        var first = await coins.GrantAsync(group["ana"], CoinReason.StreakTier, 5, group.Id, 7);
        var retry = await coins.GrantAsync(group["ana"], CoinReason.StreakTier, 5, group.Id, 7);

        first.Granted.Should().Be(5);
        retry.Granted.Should().Be(0);
        (await BalanceAsync(group["ana"])).Should().Be(5);
    }

    [Fact]
    public async Task History_IsNewestFirst_InPagesOfTwenty()
    {
        var group = await SeedGroupAsync("ana");
        await using (var db = NewContext())
        {
            var coins = NewCoinService(db);
            for (var i = 1; i <= 25; i++)
                await coins.GrantAsync(group["ana"], CoinReason.StreakTier, i, group.Id, i);
        }

        await using var read = NewContext();
        var service = NewCoinService(read);
        var page1 = await service.GetHistoryAsync(group["ana"], null);
        var page2 = await service.GetHistoryAsync(group["ana"], page1.NextBefore);

        page1.Items.Should().HaveCount(20);
        page1.Items[0].Amount.Should().Be(25);
        page1.Items[0].GroupName.Should().Be("Los de siempre");
        page1.NextBefore.Should().NotBeNull();
        page2.Items.Select(t => t.Amount).Should().Equal(5, 4, 3, 2, 1);
        page2.NextBefore.Should().BeNull();
    }
}
