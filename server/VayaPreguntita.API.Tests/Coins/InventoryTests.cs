using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.DTOs.Coins;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Coins;

// Item inventory (rama 45, §17.10): game items bought ahead in the shop, global to the user,
// and used later in a group from each item's usual spot without charging. The rules of the
// spot (freeze cap, repair window, one flash a day…) still apply when using an owned item.
[Collection(PostgresCollection.Name)]
public class InventoryTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Day1 = new(2026, 10, 5);

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

    private async Task<bool> BuyAsync(int userId, InventoryItem item)
    {
        await using var db = NewContext();
        return await NewInventoryService(db).BuyAsync(userId, item);
    }

    private async Task<InventoryDto> InventoryAsync(int userId)
    {
        await using var db = NewContext();
        return await NewInventoryService(db).GetAsync(userId);
    }

    // Puts `count` of an item straight into the inventory, without paying.
    private async Task GiveAsync(int userId, InventoryItem item, int count = 1)
    {
        await SetCoinsAsync(userId, InventoryItems.Price(item) * count);
        for (var i = 0; i < count; i++)
            (await BuyAsync(userId, item)).Should().BeTrue();
    }

    // ── Buying ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Buying_ChargesTheShopPrice_WithoutAGroup_AndAddsOne()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 250);

        (await BuyAsync(group["ana"], InventoryItem.StreakFreeze)).Should().BeTrue();
        (await BuyAsync(group["ana"], InventoryItem.StreakFreeze)).Should().BeTrue();
        (await BuyAsync(group["ana"], InventoryItem.AuthorHint)).Should().BeTrue();

        (await CoinsAsync(group["ana"])).Should().Be(250 - 2 * StreakFreezeRules.FreezePrice - CoinRewards.AuthorHintPrice);
        var inventory = await InventoryAsync(group["ana"]);
        inventory.StreakFreeze.Should().Be(2);
        inventory.AuthorHint.Should().Be(1);
        inventory.FlashQuestion.Should().Be(0);

        await using var db = NewContext();
        var rows = await db.CoinTransactions.Where(t => t.UserId == group["ana"]).ToListAsync();
        rows.Should().HaveCount(3).And.OnlyContain(t => t.GroupId == null && t.RefId == null && t.Amount < 0);
        rows.Count(t => t.Reason == CoinReason.StreakFreeze).Should().Be(2);
    }

    [Fact]
    public async Task BuyingWithoutEnoughCoins_ChangesNothing()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], CoinRewards.FlashQuestionPrice - 1);

        (await BuyAsync(group["ana"], InventoryItem.FlashQuestion)).Should().BeFalse();

        (await CoinsAsync(group["ana"])).Should().Be(CoinRewards.FlashQuestionPrice - 1);
        (await InventoryAsync(group["ana"])).FlashQuestion.Should().Be(0);
    }

    // ── Streak freeze ────────────────────────────────────────────────────────

    [Fact]
    public async Task AnOwnedFreeze_IsActivatedInAGroup_WithoutCharging()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await GiveAsync(group["ana"], InventoryItem.StreakFreeze, 2);

        await using var db = NewContext();
        var result = await NewStreakService(db).BuyFreezeAsync(group.Id, group["ana"], group["ana"], useOwned: true);

        result.Should().Be(StreakPurchaseResult.Success);
        (await CoinsAsync(group["ana"])).Should().Be(0);
        (await InventoryAsync(group["ana"])).StreakFreeze.Should().Be(1);
        // The monthly free one plus the owned one.
        (await NewStreakService(NewContext()).GetMyStreakAsync(group.Id, group["ana"])).Freezes.Should().Be(2);
    }

    [Fact]
    public async Task AnOwnedFreeze_StillRespectsTheMaxHeldPerGroup_AndIsKept()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await GiveAsync(group["ana"], InventoryItem.StreakFreeze, 2);
        await using (var db = NewContext())
            (await NewStreakService(db).BuyFreezeAsync(group.Id, group["ana"], group["ana"], useOwned: true))
                .Should().Be(StreakPurchaseResult.Success);

        await using var again = NewContext();
        var result = await NewStreakService(again).BuyFreezeAsync(group.Id, group["ana"], group["ana"], useOwned: true);

        result.Should().Be(StreakPurchaseResult.FreezeLimitReached);
        (await InventoryAsync(group["ana"])).StreakFreeze.Should().Be(1);
    }

    [Fact]
    public async Task AnOwnedFreeze_CanBeGifted()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await GiveAsync(group["ana"], InventoryItem.StreakFreeze);

        await using var db = NewContext();
        var result = await NewStreakService(db).BuyFreezeAsync(group.Id, group["ana"], group["bea"], useOwned: true);

        result.Should().Be(StreakPurchaseResult.Success);
        (await InventoryAsync(group["ana"])).StreakFreeze.Should().Be(0);
        var beas = await NewStreakService(NewContext()).GetMyStreakAsync(group.Id, group["bea"]);
        beas.Freezes.Should().Be(2);
        beas.FreezeGiftFrom.Should().Be("ana");
    }

    [Fact]
    public async Task UsingAnItemYouDontHave_FailsAndChargesNothing()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await SetCoinsAsync(group["ana"], 500);

        await using var db = NewContext();
        var result = await NewStreakService(db).BuyFreezeAsync(group.Id, group["ana"], group["ana"], useOwned: true);

        result.Should().Be(StreakPurchaseResult.NoneOwned);
        (await CoinsAsync(group["ana"])).Should().Be(500);
    }

    // ── Streak repair ────────────────────────────────────────────────────────

    [Fact]
    public async Task AnOwnedRepair_RestoresALostStreak_WithoutCharging()
    {
        // Ana votes days 1–2, misses day 3 (the free freeze saves it) and day 4 (lost).
        var group = await SeedGroupAsync("ana", "bea");
        foreach (var date in new[] { Day1, Day1.AddDays(1) })
        {
            await AddActivatedDayAsync(group.Id, date, group["bea"]);
            Clock.SetLocal(date, DailyTime.AddHours(1));
            await using var voteDb = NewContext();
            (await NewDailyService(voteDb).VoteAsync(group.Id, group["ana"], new() { SelectedTargetUserId = group["bea"] }))
                .Item1.Should().Be(VoteResult.Success);
        }
        await AddActivatedDayAsync(group.Id, Day1.AddDays(2), group["bea"]);
        var pending = await AddQuestionAsync(group.Id, text: "Pregunta del día 4", isUsed: true);
        await AddEntryAsync(group.Id, Day1.AddDays(3), pending, group["bea"], activated: false);
        foreach (var date in new[] { Day1.AddDays(3), Day1.AddDays(4) })
        {
            await AddQuestionAsync(group.Id, text: $"Pool {date:dd/MM}");
            Clock.SetLocal(date, DailyTime);
            await TickAsync();
        }
        await GiveAsync(group["ana"], InventoryItem.StreakRepair);

        await using var db = NewContext();
        var result = await NewStreakService(db).RepairAsync(group.Id, group["ana"], useOwned: true);

        result.Should().Be(StreakPurchaseResult.Success);
        (await NewStreakService(NewContext()).GetMyStreakAsync(group.Id, group["ana"])).Current.Should().Be(2);
        (await InventoryAsync(group["ana"])).StreakRepair.Should().Be(0);
        (await CoinsAsync(group["ana"])).Should().Be(0);
    }

    [Fact]
    public async Task AnOwnedRepair_IsKept_WhenThereIsNothingToRepair()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await GiveAsync(group["ana"], InventoryItem.StreakRepair);

        await using var db = NewContext();
        var result = await NewStreakService(db).RepairAsync(group.Id, group["ana"], useOwned: true);

        result.Should().Be(StreakPurchaseResult.NotRepairable);
        (await InventoryAsync(group["ana"])).StreakRepair.Should().Be(1);
    }

    // ── Author hint ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AnOwnedHint_IsSpentOnce_PerQuestion()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos", "dani");
        var questionId = await AddQuestionAsync(group.Id, creatorId: group["bea"]);
        await AddEntryAsync(group.Id, Day1, questionId, group["ana"], activated: true);
        Clock.SetLocal(Day1, DailyTime.AddHours(1));
        await using (var voteDb = NewContext())
            (await NewDailyService(voteDb).VoteAsync(group.Id, group["ana"], new() { SelectedTargetUserId = group["bea"] }))
                .Item1.Should().Be(VoteResult.Success);
        await GiveAsync(group["ana"], InventoryItem.AuthorHint, 2);

        await using var db = NewContext();
        var (result, first) = await NewAuthorGuessService(db).BuyHintAsync(group.Id, group["ana"], useOwned: true);
        await using var db2 = NewContext();
        var (_, second) = await NewAuthorGuessService(db2).BuyHintAsync(group.Id, group["ana"], useOwned: true);

        result.Should().Be(GuessResult.Success);
        first!.Guess.HintUsed.Should().BeTrue();
        first.Inventory.AuthorHint.Should().Be(1);
        first.Balance.Should().Be(0);
        second!.Inventory.AuthorHint.Should().Be(1, "the hint was already used on this question");
    }

    [Fact]
    public async Task UsingAHintYouDontHave_FailsWithoutUsingIt()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos", "dani");
        var questionId = await AddQuestionAsync(group.Id, creatorId: group["bea"]);
        await AddEntryAsync(group.Id, Day1, questionId, group["ana"], activated: true);
        Clock.SetLocal(Day1, DailyTime.AddHours(1));
        await using (var voteDb = NewContext())
            await NewDailyService(voteDb).VoteAsync(group.Id, group["ana"], new() { SelectedTargetUserId = group["bea"] });

        await using var db = NewContext();
        var (result, _) = await NewAuthorGuessService(db).BuyHintAsync(group.Id, group["ana"], useOwned: true);

        result.Should().Be(GuessResult.NoneOwned);
        await using var check = NewContext();
        (await check.AuthorGuesses.AnyAsync(g => g.HintUsed)).Should().BeFalse();
    }

    // ── Flash question ───────────────────────────────────────────────────────

    private static CreateQuestionDto Flash() =>
        new() { Text = "¿Quién se comería la última croqueta?", Type = QuestionType.Superlative };

    [Fact]
    public async Task AnOwnedFlash_LaunchesWithoutCharging_AndTheDailyLimitStillApplies()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await AddActivatedDayAsync(group.Id, Day1.AddDays(-1), group["bea"]);
        Clock.SetLocal(Day1, new TimeOnly(10, 0));
        await GiveAsync(group["ana"], InventoryItem.FlashQuestion, 2);

        await using var db = NewContext();
        var (result, response) = await NewFlashService(db).LaunchAsync(group.Id, group["ana"], Flash(), useOwned: true);
        await using var db2 = NewContext();
        var (second, _) = await NewFlashService(db2).LaunchAsync(group.Id, group["ana"], Flash(), useOwned: true);

        result.Should().Be(FlashResult.Success);
        response!.Balance.Should().Be(0);
        response.Inventory.FlashQuestion.Should().Be(1);
        second.Should().Be(FlashResult.DailyLimitReached);
        (await InventoryAsync(group["ana"])).FlashQuestion.Should().Be(1, "a launch that fails keeps the item");
    }

    [Fact]
    public async Task LaunchingWithAFlashYouDontHave_LaunchesNothing()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await AddActivatedDayAsync(group.Id, Day1.AddDays(-1), group["bea"]);
        Clock.SetLocal(Day1, new TimeOnly(10, 0));
        await SetCoinsAsync(group["ana"], 1000);

        await using var db = NewContext();
        var (result, _) = await NewFlashService(db).LaunchAsync(group.Id, group["ana"], Flash(), useOwned: true);

        result.Should().Be(FlashResult.NoneOwned);
        (await CoinsAsync(group["ana"])).Should().Be(1000);
        await using var check = NewContext();
        (await check.FlashEntries.AnyAsync()).Should().BeFalse();
    }
}
