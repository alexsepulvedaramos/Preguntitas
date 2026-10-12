using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using VayaPreguntita.API.DTOs.Coins;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Daily;

// Race to select (§17.5): at T − 10 min an unchosen question is up for grabs and the
// first member to choose wins.
[Collection(PostgresCollection.Name)]
public class RaceToSelectTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Today = new(2026, 10, 5);
    private static readonly DateOnly Tomorrow = Today.AddDays(1);
    private static readonly TimeOnly RaceTime = DailyTime.AddMinutes(-10);

    // Today's question is open; tomorrow's was preselected automatically and carlos was
    // drawn to choose it.
    private async Task<(SeededGroup Group, int AutoPickId)> SeedPendingSelectionAsync()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        await AddActivatedDayAsync(group.Id, Today, group["bea"]);
        var autoPick = await AddQuestionAsync(group.Id, text: "La automática");
        await AddEntryAsync(group.Id, Tomorrow, autoPick, group["carlos"], activated: false);
        return (group, autoPick);
    }

    private async Task<SeededGroup> SeedOpenRaceAsync()
    {
        var (group, _) = await SeedPendingSelectionAsync();
        Clock.SetLocal(Tomorrow, RaceTime);
        await TickAsync();
        return group;
    }

    private async Task<(SelectResult Result, CoinsEarnedDto? Coins)> ConfirmAsync(int groupId, int userId)
    {
        await using var db = NewContext();
        return await NewDailyService(db).ConfirmSelectionAsync(groupId, userId);
    }

    private async Task<(SelectResult Result, CoinsEarnedDto? Coins)> SelectAsync(
        int groupId,
        int userId,
        SelectQuestionDto dto
    )
    {
        await using var db = NewContext();
        return await NewDailyService(db).SelectQuestionAsync(groupId, userId, dto);
    }

    private async Task<DailyStatusDto> StatusAsync(int groupId, int userId)
    {
        await using var db = NewContext();
        return await NewDailyService(db).GetCurrentStatusAsync(groupId, userId);
    }

    [Fact]
    public async Task TenMinutesBeforeT_AnUnchosenQuestionOpensTheRaceForEveryoneButTheSelector()
    {
        var (group, _) = await SeedPendingSelectionAsync();

        Clock.SetLocal(Tomorrow, RaceTime.AddMinutes(-1));
        await TickAsync();
        (await StatusAsync(group.Id, group["ana"])).Selection!.RaceOpen.Should().BeFalse();

        Clock.SetLocal(Tomorrow, RaceTime);
        await TickAsync();
        await TickAsync();

        Notifications.Verify(
            n => n.SendRaceToSelectAsync(group.Id, group["carlos"], "Los de siempre", DailyTime),
            Times.Once
        );
        var view = (await StatusAsync(group.Id, group["ana"])).Selection!;
        view.RaceOpen.Should().BeTrue();
        view.IsCurrentUserSelector.Should().BeFalse();
        view.SelectorUserId.Should().BeNull("who was drawn still isn't told to the others");
        view.PendingQuestion!.Text.Should().Be("La automática", "racers may keep it, so they see it");
    }

    [Fact]
    public async Task ASelectorWhoAlreadyChose_NeverOpensTheRace()
    {
        var (group, _) = await SeedPendingSelectionAsync();
        Clock.SetLocal(Tomorrow, new TimeOnly(12, 0));
        await ConfirmAsync(group.Id, group["carlos"]);

        Clock.SetLocal(Tomorrow, RaceTime);
        await TickAsync();

        Notifications.Verify(
            n => n.SendRaceToSelectAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TimeOnly>()),
            Times.Never
        );
        (await StatusAsync(group.Id, group["ana"])).Selection!.PendingQuestion.Should().BeNull();
    }

    [Fact]
    public async Task BeforeTheRace_OnlyTheSelectorMayChoose()
    {
        var (group, _) = await SeedPendingSelectionAsync();
        Clock.SetLocal(Tomorrow, new TimeOnly(12, 0));

        (await ConfirmAsync(group.Id, group["ana"])).Result.Should().Be(SelectResult.NotYourTurn);
    }

    [Fact]
    public async Task TheFirstMemberToChooseWinsTheTurnAndTenCoins_TheRestAreTooLate()
    {
        var group = await SeedOpenRaceAsync();

        var ana = await ConfirmAsync(group.Id, group["ana"]);
        var bea = await ConfirmAsync(group.Id, group["bea"]);
        var carlos = await ConfirmAsync(group.Id, group["carlos"]);

        ana.Result.Should().Be(SelectResult.Success);
        ana.Coins!.Earned.Should().Be(CoinRewards.RaceWon);
        bea.Result.Should().Be(SelectResult.RaceLost);
        carlos.Result.Should().Be(SelectResult.RaceLost, "losing the race spends the selector's turn too");
        await using var db = NewContext();
        var entry = await db.DailyEntries.SingleAsync(d => d.Date == Tomorrow);
        entry.SelectorUserId.Should().Be(group["ana"]);
        entry.SelectionKind.Should().Be(SelectionKind.Confirmed);
        (await db.CoinTransactions.SingleAsync(t => t.Reason == CoinReason.RaceWon)).UserId.Should().Be(group["ana"]);
        (await StatusAsync(group.Id, group["ana"])).Selection!.IsCurrentUserSelector.Should().BeTrue();
    }

    [Fact]
    public async Task TheWinnerAlsoEarnsTheActiveChoiceRewardAtT_AndKeepsTheirOwnRoundTurn()
    {
        var group = await SeedOpenRaceAsync();
        var poolQuestion = await AddQuestionAsync(group.Id, text: "La de bea", creatorId: group["ana"]);

        var bea = await SelectAsync(group.Id, group["bea"], new() { ExistingQuestionId = poolQuestion });
        await using (var before = NewContext())
            (await before.GroupMembers.SingleAsync(m => m.UserId == group["bea"])).LastSelectorRound
                .Should()
                .Be(0, "a race win is an extra turn, not the member's own");
        Clock.SetLocal(Tomorrow, DailyTime);
        await TickAsync();

        bea.Result.Should().Be(SelectResult.Success);
        await using var db = NewContext();
        var entry = await db.DailyEntries.SingleAsync(d => d.Date == Tomorrow);
        entry.QuestionId.Should().Be(poolQuestion);
        entry.SelectionKind.Should().Be(SelectionKind.Picked);
        entry.ActivatedAt.Should().NotBeNull();
        (await db.CoinTransactions.Where(t => t.UserId == group["bea"]).SumAsync(t => t.Amount))
            .Should()
            .Be(CoinRewards.RaceWon + CoinRewards.SelectorChoice);
    }

    [Fact]
    public async Task TheSelectorChoosingDuringTheRace_KeepsTheTurnWithoutTheRaceReward()
    {
        var group = await SeedOpenRaceAsync();

        var carlos = await ConfirmAsync(group.Id, group["carlos"]);
        var ana = await ConfirmAsync(group.Id, group["ana"]);

        carlos.Result.Should().Be(SelectResult.Success);
        carlos.Coins.Should().BeNull();
        ana.Result.Should().Be(SelectResult.RaceLost);
        await using var db = NewContext();
        (await db.DailyEntries.SingleAsync(d => d.Date == Tomorrow)).SelectorUserId.Should().Be(group["carlos"]);
        (await db.CoinTransactions.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task AnInvalidChoiceDuringTheRace_LeavesTheRaceOpen()
    {
        var group = await SeedOpenRaceAsync();

        var ana = await SelectAsync(group.Id, group["ana"], new() { ExistingQuestionId = 999_999 });

        ana.Result.Should().Be(SelectResult.QuestionNotFound);
        await using var db = NewContext();
        (await db.DailyEntries.SingleAsync(d => d.Date == Tomorrow)).SelectorUserId.Should().Be(group["carlos"]);
        (await StatusAsync(group.Id, group["bea"])).Selection!.RaceOpen.Should().BeTrue();
    }

    [Fact]
    public async Task SimultaneousClaims_HaveExactlyOneWinner()
    {
        var group = await SeedOpenRaceAsync();

        var results = await Task.WhenAll(
            ConfirmAsync(group.Id, group["ana"]),
            ConfirmAsync(group.Id, group["bea"]),
            ConfirmAsync(group.Id, group["carlos"])
        );

        results.Count(r => r.Result == SelectResult.Success).Should().Be(1);
        results.Count(r => r.Result == SelectResult.RaceLost).Should().Be(2);
        await using var db = NewContext();
        (await db.CoinTransactions.CountAsync(t => t.Reason == CoinReason.RaceWon)).Should().BeLessThanOrEqualTo(1);
    }
}
