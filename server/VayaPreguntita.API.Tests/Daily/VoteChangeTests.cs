using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Daily;

// "Change my vote" (rama 43, §17.10): once per question while it is open, daily or flash, for
// CoinRewards.VoteChangePrice or one from the inventory. The first-voter reward, the streak
// and the coins the vote earned stay as they were.
[Collection(PostgresCollection.Name)]
public class VoteChangeTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    // Today's question opened at T; ana has coins to spare.
    private async Task<(SeededGroup Group, int QuestionId)> SeedAsync(int coins = 100)
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        var (_, questionId) = await AddActivatedDayAsync(group.Id, Today, group["carlos"]);
        Clock.SetLocal(Today, DailyTime.AddHours(1));
        await using var db = NewContext();
        await db.Users.Where(u => u.Id == group["ana"]).ExecuteUpdateAsync(s => s.SetProperty(u => u.Coins, coins));
        return (group, questionId);
    }

    private async Task VoteAsync(SeededGroup group, string voter, string target)
    {
        await using var db = NewContext();
        var (result, _) = await NewDailyService(db)
            .VoteAsync(group.Id, group[voter], new() { SelectedTargetUserId = group[target] });
        result.Should().Be(VoteResult.Success);
    }

    private async Task<(VoteChangeResult Result, ChangeVoteResponseDto? Response)> ChangeAsync(
        SeededGroup group,
        string voter,
        string target,
        bool useOwned = false
    )
    {
        await using var db = NewContext();
        return await NewVoteChangeService(db)
            .ChangeDailyAsync(group.Id, group[voter], new CreateVoteDto { SelectedTargetUserId = group[target] }, useOwned);
    }

    private async Task<int> CoinsAsync(int userId)
    {
        await using var db = NewContext();
        return await db.Users.Where(u => u.Id == userId).Select(u => u.Coins).SingleAsync();
    }

    [Fact]
    public async Task Changing_ReplacesTheAnswer_AndChargesThePrice()
    {
        var (group, questionId) = await SeedAsync();
        await VoteAsync(group, "ana", "bea");
        var afterVote = await CoinsAsync(group["ana"]);

        var (result, response) = await ChangeAsync(group, "ana", "carlos");

        result.Should().Be(VoteChangeResult.Success);
        response!.Balance.Should().Be(afterVote - CoinRewards.VoteChangePrice);
        response.Results!.Results.Single(r => r.TargetUser?.Id == group["carlos"]).VoteCount.Should().Be(1);
        response.Results.Results.Where(r => r.TargetUser?.Id == group["bea"]).Sum(r => r.VoteCount).Should().Be(0);

        await using var db = NewContext();
        (await db.Votes.SingleAsync(v => v.QuestionId == questionId && v.UserId == group["ana"]))
            .SelectedTargetUserId.Should().Be(group["carlos"]);
        var charge = await db.CoinTransactions.SingleAsync(t => t.Reason == CoinReason.VoteChange);
        charge.Amount.Should().Be(-CoinRewards.VoteChangePrice);
        charge.RefId.Should().Be(questionId);
    }

    [Fact]
    public async Task Changing_KeepsTheFirstVoterRewardAndTheStreak()
    {
        var (group, _) = await SeedAsync();
        await VoteAsync(group, "ana", "bea");
        await VoteAsync(group, "bea", "ana");

        await ChangeAsync(group, "ana", "carlos");

        await using var db = NewContext();
        var entry = await db.DailyEntries.SingleAsync(d => d.GroupId == group.Id);
        entry.FirstVoterUserId.Should().Be(group["ana"]);
        (await db.GroupMembers.SingleAsync(m => m.GroupId == group.Id && m.UserId == group["ana"]))
            .CurrentStreak.Should().Be(1);
        (await db.CoinTransactions.CountAsync(t => t.UserId == group["ana"] && t.Reason == CoinReason.Vote))
            .Should().Be(1);
    }

    [Fact]
    public async Task OnlyOnce_AndOnlyAfterVoting()
    {
        var (group, _) = await SeedAsync(coins: 500);

        (await ChangeAsync(group, "ana", "bea")).Result.Should().Be(VoteChangeResult.NotVoted);

        await VoteAsync(group, "ana", "bea");
        (await ChangeAsync(group, "ana", "carlos")).Result.Should().Be(VoteChangeResult.Success);
        (await ChangeAsync(group, "ana", "bea")).Result.Should().Be(VoteChangeResult.AlreadyChanged);

        await using var db = NewContext();
        var status = await NewDailyService(db).GetCurrentStatusAsync(group.Id, group["ana"]);
        status.Today.CanChangeVote.Should().BeFalse();
    }

    [Fact]
    public async Task TheResultsCard_OffersIt_OnceYouHaveVoted()
    {
        var (group, _) = await SeedAsync();
        await VoteAsync(group, "ana", "bea");

        await using var db = NewContext();
        (await NewDailyService(db).GetCurrentStatusAsync(group.Id, group["ana"])).Today.CanChangeVote.Should().BeTrue();
        (await NewDailyService(db).GetCurrentStatusAsync(group.Id, group["bea"])).Today.CanChangeVote.Should().BeFalse();
    }

    [Fact]
    public async Task WithoutEnoughCoins_NothingChanges()
    {
        var (group, questionId) = await SeedAsync(coins: 0);
        await VoteAsync(group, "ana", "bea");
        await using (var db = NewContext())
            await db.Users.Where(u => u.Id == group["ana"]).ExecuteUpdateAsync(s => s.SetProperty(u => u.Coins, 10));

        (await ChangeAsync(group, "ana", "carlos")).Result.Should().Be(VoteChangeResult.NotEnoughCoins);

        await using var check = NewContext();
        (await check.Votes.SingleAsync(v => v.QuestionId == questionId && v.UserId == group["ana"]))
            .SelectedTargetUserId.Should().Be(group["bea"]);
        (await check.VoteChanges.AnyAsync()).Should().BeFalse();
        (await CoinsAsync(group["ana"])).Should().Be(10);
    }

    [Fact]
    public async Task AnOwnedItem_IsUsedInsteadOfCoins()
    {
        var (group, _) = await SeedAsync();
        await VoteAsync(group, "ana", "bea");
        await using (var db = NewContext())
            (await NewInventoryService(db).BuyAsync(group["ana"], InventoryItem.VoteChange)).Should().BeTrue();
        var before = await CoinsAsync(group["ana"]);

        var (result, response) = await ChangeAsync(group, "ana", "carlos", useOwned: true);

        result.Should().Be(VoteChangeResult.Success);
        response!.Balance.Should().Be(before);
        response.Inventory.VoteChange.Should().Be(0);
    }

    [Fact]
    public async Task AnInvalidAnswer_IsRejected()
    {
        var (group, _) = await SeedAsync();
        await VoteAsync(group, "ana", "bea");

        await using var db = NewContext();
        var (result, _) = await NewVoteChangeService(db)
            .ChangeDailyAsync(group.Id, group["ana"], new CreateVoteDto { SelectedTargetUserId = 999_999 }, false);
        result.Should().Be(VoteChangeResult.InvalidVote);
    }

    [Fact]
    public async Task AFlashVote_CanBeChangedWhileOpen_ButNotOnceClosed()
    {
        var (group, _) = await SeedAsync(coins: 500);
        int flashId;
        await using (var db = NewContext())
        {
            var (_, launched) = await NewFlashService(db)
                .LaunchAsync(group.Id, group["ana"], new() { Text = "¿Quién ronca más?", Type = QuestionType.Superlative });
            flashId = launched!.Flash.Id;
        }
        await using (var db = NewContext())
            await NewFlashService(db).VoteAsync(group.Id, group["ana"], flashId, new() { SelectedTargetUserId = group["bea"] });

        await using (var db = NewContext())
        {
            var (result, response) = await NewVoteChangeService(db)
                .ChangeFlashAsync(group.Id, group["ana"], flashId, new() { SelectedTargetUserId = group["carlos"] }, false);
            result.Should().Be(VoteChangeResult.Success);
            response!.Flash!.CanChangeVote.Should().BeFalse();
        }

        Clock.Advance(TimeSpan.FromHours(25));
        await using (var db = NewContext())
        {
            var (result, _) = await NewVoteChangeService(db)
                .ChangeFlashAsync(group.Id, group["bea"], flashId, new() { SelectedTargetUserId = group["ana"] }, false);
            result.Should().Be(VoteChangeResult.FlashClosed);
        }
    }
}
