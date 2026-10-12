using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using VayaPreguntita.API.DTOs.Streaks;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Streaks;

// Streak freeze & repair (rama 35, §17.9): one free freeze per month per group, granted
// lazily; the close-of-cycle tick spends one when a member misses an active cycle, or
// records the loss so it can be bought back until the next cycle closes.
[Collection(PostgresCollection.Name)]
public class StreakFreezeTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Day1 = new(2026, 10, 5);

    // Ana votes days 1–2 (streak 2), day 3 is open, day 4 is preselected.
    private async Task<SeededGroup> SeedTwoDayStreakAsync()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await OpenAndVoteAsync(group, Day1);
        await OpenAndVoteAsync(group, Day1.AddDays(1));
        await AddActivatedDayAsync(group.Id, Day1.AddDays(2), group["bea"]);
        var questionId = await AddQuestionAsync(group.Id, text: "Pregunta del día 4", isUsed: true);
        await AddEntryAsync(group.Id, Day1.AddDays(3), questionId, group["bea"], activated: false);
        return group;
    }

    private async Task OpenAndVoteAsync(SeededGroup group, DateOnly date)
    {
        await AddActivatedDayAsync(group.Id, date, group["bea"]);
        await VoteAsync(group, date);
    }

    private async Task VoteAsync(SeededGroup group, DateOnly date)
    {
        Clock.SetLocal(date, DailyTime.AddHours(1));
        await using var db = NewContext();
        var (result, _) = await NewDailyService(db)
            .VoteAsync(group.Id, group["ana"], new() { SelectedTargetUserId = group["bea"] });
        result.Should().Be(VoteResult.Success);
    }

    // The tick at `date`@T activates that day's (preselected) entry, closing the previous
    // cycle, and preselects the next day from a fresh pool question.
    private async Task ActivateAsync(SeededGroup group, DateOnly date)
    {
        await AddQuestionAsync(group.Id, text: $"Pool {date:dd/MM}");
        Clock.SetLocal(date, DailyTime);
        await TickAsync();
    }

    private async Task<MyStreakDto> AnasStreakAsync(SeededGroup group)
    {
        await using var db = NewContext();
        return await NewStreakService(db).GetMyStreakAsync(group.Id, group["ana"]);
    }

    private async Task SetCoinsAsync(int userId, int coins)
    {
        await using var db = NewContext();
        await db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.Coins, coins));
    }

    private async Task<int> CoinsAsync(int userId)
    {
        await using var db = NewContext();
        return await db.Users.Where(u => u.Id == userId).Select(u => u.Coins).SingleAsync();
    }

    private async Task<StreakPurchaseResult> BuyFreezeAsync(SeededGroup group, string buyer, string receiver)
    {
        await using var db = NewContext();
        return await NewStreakService(db).BuyFreezeAsync(group.Id, group[buyer], group[receiver]);
    }

    private async Task<StreakPurchaseResult> RepairAsync(SeededGroup group)
    {
        await using var db = NewContext();
        return await NewStreakService(db).RepairAsync(group.Id, group["ana"]);
    }

    [Fact]
    public async Task EveryMemberGetsOneFreeFreezePerMonth_GrantedWhenRead()
    {
        var group = await SeedGroupAsync("ana", "bea");

        var first = await AnasStreakAsync(group);
        var again = await AnasStreakAsync(group);

        first.Freezes.Should().Be(1);
        again.Freezes.Should().Be(1, "the free freeze is granted once per month");
        first.MaxFreezes.Should().Be(StreakFreezeRules.MaxHeld);
    }

    [Fact]
    public async Task MissingACycle_SpendsTheFreeze_AndKeepsTheStreakWithoutAddingTheDay()
    {
        var group = await SeedTwoDayStreakAsync();

        await ActivateAsync(group, Day1.AddDays(3));

        var streak = await AnasStreakAsync(group);
        streak.Current.Should().Be(2);
        streak.Freezes.Should().Be(0);
        streak.FreezeSavedStreak.Should().Be(2);
        streak.LostStreak.Should().BeNull();

        await VoteAsync(group, Day1.AddDays(3));
        (await AnasStreakAsync(group)).Current.Should().Be(3);
    }

    [Fact]
    public async Task MissingWithoutAFreeze_LosesTheStreak_AndOffersARepair()
    {
        var group = await SeedTwoDayStreakAsync();
        await ActivateAsync(group, Day1.AddDays(3)); // day 3 missed → the free freeze saves it
        await ActivateAsync(group, Day1.AddDays(4)); // day 4 missed → no freeze left

        var streak = await AnasStreakAsync(group);
        streak.Current.Should().Be(0);
        streak.LostStreak.Should().Be(2);
        streak.RepairableStreak.Should().Be(2);
    }

    [Fact]
    public async Task Repairing_RestoresTheLostStreak_AndCharges300()
    {
        var group = await SeedTwoDayStreakAsync();
        await ActivateAsync(group, Day1.AddDays(3));
        await ActivateAsync(group, Day1.AddDays(4));
        await SetCoinsAsync(group["ana"], 350);

        (await RepairAsync(group)).Should().Be(StreakPurchaseResult.Success);

        var streak = await AnasStreakAsync(group);
        streak.Current.Should().Be(2);
        streak.LostStreak.Should().BeNull();
        streak.RepairableStreak.Should().BeNull();
        (await CoinsAsync(group["ana"])).Should().Be(50);

        await VoteAsync(group, Day1.AddDays(4));
        (await AnasStreakAsync(group)).Current.Should().Be(3);
    }

    [Fact]
    public async Task RepairingAfterVotingToday_AddsTodaysVoteOnTop()
    {
        var group = await SeedTwoDayStreakAsync();
        await ActivateAsync(group, Day1.AddDays(3));
        await ActivateAsync(group, Day1.AddDays(4));
        await VoteAsync(group, Day1.AddDays(4)); // a new streak of 1
        await SetCoinsAsync(group["ana"], 300);

        (await RepairAsync(group)).Should().Be(StreakPurchaseResult.Success);

        (await AnasStreakAsync(group)).Current.Should().Be(3);
    }

    [Fact]
    public async Task TheRepairWindowCloses_WithTheNextCycle()
    {
        var group = await SeedTwoDayStreakAsync();
        await ActivateAsync(group, Day1.AddDays(3));
        await ActivateAsync(group, Day1.AddDays(4));
        await ActivateAsync(group, Day1.AddDays(5));
        await SetCoinsAsync(group["ana"], 300);

        (await AnasStreakAsync(group)).RepairableStreak.Should().BeNull();
        (await RepairAsync(group)).Should().Be(StreakPurchaseResult.NotRepairable);
        (await CoinsAsync(group["ana"])).Should().Be(300);
    }

    [Fact]
    public async Task RepairWithoutEnoughCoins_ChangesNothing()
    {
        var group = await SeedTwoDayStreakAsync();
        await ActivateAsync(group, Day1.AddDays(3));
        await ActivateAsync(group, Day1.AddDays(4));
        await SetCoinsAsync(group["ana"], 299);

        (await RepairAsync(group)).Should().Be(StreakPurchaseResult.NotEnoughCoins);

        (await AnasStreakAsync(group)).RepairableStreak.Should().Be(2);
        (await CoinsAsync(group["ana"])).Should().Be(299);
    }

    [Fact]
    public async Task BuyingFreezes_StopsAtTheLimit_AndChargesOnlyWhatWasBought()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 250);

        (await BuyFreezeAsync(group, "ana", "ana")).Should().Be(StreakPurchaseResult.Success);
        (await BuyFreezeAsync(group, "ana", "ana")).Should().Be(StreakPurchaseResult.FreezeLimitReached);

        (await AnasStreakAsync(group)).Freezes.Should().Be(2, "the free one plus the bought one");
        (await CoinsAsync(group["ana"])).Should().Be(150);

        await using var db = NewContext();
        var ledger = await db.CoinTransactions.SingleAsync();
        (ledger.Amount, ledger.Reason, ledger.GroupId).Should().Be((-100, CoinReason.StreakFreeze, group.Id));
    }

    [Fact]
    public async Task BuyingAFreezeWithoutEnoughCoins_Fails()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 99);

        (await BuyFreezeAsync(group, "ana", "ana")).Should().Be(StreakPurchaseResult.NotEnoughCoins);

        (await AnasStreakAsync(group)).Freezes.Should().Be(1);
        (await CoinsAsync(group["ana"])).Should().Be(99);
    }

    [Fact]
    public async Task GiftingAFreeze_ChargesTheGiver_AndTellsTheReceiver()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["bea"], 100);

        (await BuyFreezeAsync(group, "bea", "ana")).Should().Be(StreakPurchaseResult.Success);

        var streak = await AnasStreakAsync(group);
        streak.Freezes.Should().Be(2);
        streak.FreezeGiftFrom.Should().Be("bea");
        (await CoinsAsync(group["bea"])).Should().Be(0);

        await using (var db = NewContext())
            await NewStreakService(db).DismissGiftNoticeAsync(group.Id, group["ana"]);
        (await AnasStreakAsync(group)).FreezeGiftFrom.Should().BeNull();
    }

    [Fact]
    public async Task GiftingToSomeoneAtTheLimit_FailsWithoutCharging()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 100);
        await SetCoinsAsync(group["bea"], 100);
        await BuyFreezeAsync(group, "ana", "ana");

        (await BuyFreezeAsync(group, "bea", "ana")).Should().Be(StreakPurchaseResult.FreezeLimitReached);
        (await CoinsAsync(group["bea"])).Should().Be(100);
    }

    [Fact]
    public async Task GiftingToSomeoneOutsideTheGroup_IsRejected()
    {
        var group = await SeedGroupAsync("ana", "bea");
        var other = await SeedGroupAsync("carlos", "dani");
        await SetCoinsAsync(group["ana"], 100);

        await using var db = NewContext();
        var result = await NewStreakService(db).BuyFreezeAsync(group.Id, group["ana"], other["carlos"]);

        result.Should().Be(StreakPurchaseResult.InvalidTarget);
        (await CoinsAsync(group["ana"])).Should().Be(100);
    }

    [Fact]
    public async Task AVoteThatRacesTheTick_GetsItsFreezeBack()
    {
        var group = await SeedTwoDayStreakAsync();
        await ActivateAsync(group, Day1.AddDays(3)); // the tick froze day 3 for ana

        // Ana's vote on day 3 lands right after the tick.
        await using (var db = NewContext())
        {
            var day3 = await db.DailyEntries.SingleAsync(d => d.GroupId == group.Id && d.Date == Day1.AddDays(2));
            await NewStreakService(db).RegisterVoteAsync(group.Id, group["ana"], day3);
        }

        var streak = await AnasStreakAsync(group);
        streak.Current.Should().Be(3);
        streak.Freezes.Should().Be(1);
        streak.FreezeSavedStreak.Should().BeNull();
    }

    [Fact]
    public async Task TheDangerPush_SaysWhenAFreezeWillCoverTheMiss()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await OpenAndVoteAsync(group, Day1);
        await OpenAndVoteAsync(group, Day1.AddDays(1));
        await OpenAndVoteAsync(group, Day1.AddDays(2));
        await AddActivatedDayAsync(group.Id, Day1.AddDays(3), group["bea"]);

        Clock.SetLocal(Day1.AddDays(4), DailyTime.AddHours(-1));
        await using var db = NewContext();
        await NewStreakService(db).SendDangerRemindersAsync();

        Notifications.Verify(n =>
            n.SendStreakDangerAsync(group.Id, group["ana"], It.IsAny<string>(), 3, DailyTime, true)
        );
    }
}
