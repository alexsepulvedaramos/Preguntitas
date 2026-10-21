using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Services;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Daily;

// Flash variants (rama 50, §17.15 item 6): the anonymous and time-capsule add-ons on top of a
// flash, combinable and paid in coins, and the +24 h extension any member can buy once.
[Collection(PostgresCollection.Name)]
public class FlashVariantsTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Today = new(2026, 10, 5);
    private static readonly TimeOnly Morning = new(10, 0);

    // Today's daily question is open; ana and bea have coins, carlos has none.
    private async Task<SeededGroup> SeedAsync(int coins = 600)
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        await AddActivatedDayAsync(group.Id, Today.AddDays(-1), group["carlos"]);
        Clock.SetLocal(Today, Morning);
        await using var db = NewContext();
        await db.Users.Where(u => u.Id == group["ana"] || u.Id == group["bea"])
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Coins, coins));
        return group;
    }

    private async Task<(FlashResult Result, LaunchFlashResponseDto? Response)> LaunchAsync(
        SeededGroup group,
        string launcher,
        FlashOptions options,
        QuestionType type = QuestionType.OpenText
    )
    {
        await using var db = NewContext();
        return await NewFlashService(db).LaunchAsync(
            group.Id,
            group[launcher],
            new CreateQuestionDto { Text = "¿Qué opinas de verdad del jefe?", Type = type },
            options: options
        );
    }

    private async Task<FlashStateDto> VoteTextAsync(SeededGroup group, string voter, int flashId, string text)
    {
        await using var db = NewContext();
        var (result, state) = await NewFlashService(db).VoteAsync(group.Id, group[voter], flashId, new() { FreeText = text });
        result.Should().Be(FlashResult.Success);
        return state!;
    }

    private async Task<(FlashResult Result, ExtendFlashResponseDto? Response)> ExtendAsync(
        SeededGroup group, string member, int flashId)
    {
        await using var db = NewContext();
        return await NewFlashService(db).ExtendAsync(group.Id, group[member], flashId);
    }

    private async Task<int> CoinsAsync(SeededGroup group, string member)
    {
        await using var db = NewContext();
        return await db.Users.Where(u => u.Id == group[member]).Select(u => u.Coins).SingleAsync();
    }

    // ── Anonymous ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnAnonymousFlash_CostsTheAddOn_AndNamesNobody()
    {
        var group = await SeedAsync();

        var (result, response) = await LaunchAsync(group, "ana", new FlashOptions(Anonymous: true));

        result.Should().Be(FlashResult.Success);
        response!.Balance.Should().Be(600 - CoinRewards.FlashQuestionPrice - CoinRewards.FlashAnonymousPrice);
        response.Flash.IsAnonymous.Should().BeTrue();
        response.Flash.LaunchedBy.Should().BeNull();
        Notifications.Verify(
            n => n.SendFlashQuestionAsync(group.Id, group["ana"], It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), null, true),
            Times.Once
        );

        await using (var db = NewContext())
        {
            var reasons = await db.CoinTransactions.Where(c => c.UserId == group["ana"]).Select(c => c.Reason).ToListAsync();
            reasons.Should().BeEquivalentTo([CoinReason.FlashQuestion, CoinReason.FlashAnonymous]);
        }

        var flashId = response.Flash.Id;
        await VoteTextAsync(group, "bea", flashId, "Que es majo");
        var state = await VoteTextAsync(group, "carlos", flashId, "Mejor no digo nada");

        state.VoterIds.Should().BeEmpty("nobody may see who has voted");
        state.VoteCount.Should().Be(2);
        state.Results!.Anonymous.Should().BeTrue();
        var answers = state.Results.FreeTextResponses;
        answers.Should().HaveCount(2);
        answers.Single(a => a.Text == "Que es majo").UserId.Should().Be(0);
        answers.Single(a => a.Text == "Que es majo").Username.Should().BeEmpty();
        answers.Single(a => a.Text == "Mejor no digo nada").UserId.Should().Be(group["carlos"], "only you know which is yours");

        await using (var db = NewContext())
        {
            var nudges = new NudgeService(db, NewCoinService(db), NewInventoryService(db), Notifications.Object, Clock);
            (await nudges.GetTargetsAsync(group.Id, group["bea"], flashId)).Should().BeNull("nudges would tell who is missing");
        }

        Clock.SetLocal(Today.AddDays(1), Morning.AddMinutes(1));
        await using (var db = NewContext())
        {
            var history = await new QuestionsService(db, TestMapper.Instance, Clock).GetHistoryAsync(group.Id, group["bea"], null);
            var item = history.Items.Single(i => i.FlashId == flashId);
            item.AuthorUsername.Should().BeNull();
            item.IsAnonymous.Should().BeTrue();
            var detail = await NewFlashService(db).GetDetailAsync(group.Id, group["bea"], flashId);
            detail!.Author.Should().BeNull();
            detail.FreeTextResponses.Should().OnlyContain(a => a.UserId == 0 || a.UserId == group["bea"]);
        }
    }

    [Fact]
    public async Task AnAnonymousSuperlative_KeepsTheTallies_ButNoVoters()
    {
        var group = await SeedAsync();
        var (_, response) = await LaunchAsync(group, "ana", new FlashOptions(Anonymous: true), QuestionType.Superlative);

        await using var db = NewContext();
        var (_, state) = await NewFlashService(db)
            .VoteAsync(group.Id, group["bea"], response!.Flash.Id, new() { SelectedTargetUserId = group["carlos"] });

        var option = state!.Results!.Results.Single();
        option.VoteCount.Should().Be(1);
        option.Voters.Should().BeEmpty();
    }

    // ── Time capsule ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public async Task ACapsule_OpensBetweenTomorrowAndAYear(int days)
    {
        var group = await SeedAsync();

        var (result, _) = await LaunchAsync(group, "ana", new FlashOptions(CapsuleOpensOn: Today.AddDays(days)));

        result.Should().Be(FlashResult.InvalidCapsuleDate);
        (await CoinsAsync(group, "ana")).Should().Be(600);
        await using var db = NewContext();
        (await db.FlashEntries.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task ACapsule_HidesItsResultsFromEveryone_UntilItOpens_ThenPushesOnce()
    {
        var group = await SeedAsync();
        var opensOn = Today.AddDays(30);

        var (result, response) = await LaunchAsync(group, "ana", new FlashOptions(CapsuleOpensOn: opensOn));

        result.Should().Be(FlashResult.Success);
        response!.Balance.Should().Be(600 - CoinRewards.FlashQuestionPrice - CoinRewards.FlashCapsulePrice);
        response.Flash.CapsuleOpensAt.Should().Be(response.Flash.ActivatedAt.AddDays(30));
        var flashId = response.Flash.Id;

        await VoteTextAsync(group, "ana", flashId, "Le echaré de menos");
        var state = await VoteTextAsync(group, "bea", flashId, "Ni fu ni fa");
        state.Sealed.Should().BeTrue();
        state.Results.Should().BeNull("not even its voters see the results");
        state.VoteCount.Should().Be(2);

        // Closed but still sealed: the history shows only the count.
        Clock.SetLocal(Today.AddDays(2), Morning);
        await TickAsync();
        await using (var db = NewContext())
        {
            var detail = await NewFlashService(db).GetDetailAsync(group.Id, group["ana"], flashId);
            detail!.Sealed.Should().BeTrue();
            detail.Locked.Should().BeFalse();
            detail.Results.Should().BeEmpty();
            detail.FreeTextResponses.Should().BeEmpty();
            detail.TotalVotes.Should().Be(2);
        }
        Notifications.Verify(
            n => n.SendCapsuleOpenedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<DateOnly>(), It.IsAny<string>()),
            Times.Never
        );

        Clock.SetLocal(opensOn, Morning.AddMinutes(1));
        await TickAsync();
        await TickAsync();

        Notifications.Verify(
            n => n.SendCapsuleOpenedAsync(group.Id, flashId, "Los de siempre", "ana", Today, It.IsAny<string>()),
            Times.Once
        );
        await using (var db = NewContext())
        {
            var detail = await NewFlashService(db).GetDetailAsync(group.Id, group["bea"], flashId);
            detail!.Sealed.Should().BeFalse();
            detail.FreeTextResponses.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task BothAddOns_Combine_AndAreCheckedBeforeCharging()
    {
        var group = await SeedAsync(coins: 150);
        var both = new FlashOptions(Anonymous: true, CapsuleOpensOn: Today.AddDays(1));

        var (poor, _) = await LaunchAsync(group, "ana", both);
        poor.Should().Be(FlashResult.NotEnoughCoins);
        (await CoinsAsync(group, "ana")).Should().Be(150);

        await using (var db = NewContext())
            await db.Users.Where(u => u.Id == group["ana"]).ExecuteUpdateAsync(s => s.SetProperty(u => u.Coins, 200));
        var (result, response) = await LaunchAsync(group, "ana", both);

        result.Should().Be(FlashResult.Success);
        response!.Balance.Should().Be(0);
        response.Flash.IsAnonymous.Should().BeTrue();
        response.Flash.Sealed.Should().BeTrue();
    }

    // ── Extension ────────────────────────────────────────────────────────────

    [Fact]
    public async Task AnyMember_CanExtendAnOpenFlash_Once()
    {
        var group = await SeedAsync();
        var (_, launched) = await LaunchAsync(group, "ana", new FlashOptions());
        var flashId = launched!.Flash.Id;

        var (result, response) = await ExtendAsync(group, "bea", flashId);

        result.Should().Be(FlashResult.Success);
        response!.Balance.Should().Be(600 - CoinRewards.FlashExtensionPrice);
        response.Flash.ClosesAt.Should().Be(launched.Flash.ClosesAt + TimeSpan.FromHours(24));
        response.Flash.Extended.Should().BeTrue();
        response.Flash.ExtendedBy!.Username.Should().Be("bea");

        var (again, _) = await ExtendAsync(group, "ana", flashId);
        again.Should().Be(FlashResult.AlreadyExtended);
        (await CoinsAsync(group, "ana")).Should().Be(600 - CoinRewards.FlashQuestionPrice);

        await using var db = NewContext();
        var charge = await db.CoinTransactions.SingleAsync(c => c.UserId == group["bea"]);
        charge.Reason.Should().Be(CoinReason.FlashExtension);
        charge.RefId.Should().Be(flashId);

        // Still open the day after, when it would have closed.
        Clock.SetLocal(Today.AddDays(1), Morning.AddMinutes(1));
        (await NewFlashService(db).GetOpenAsync(group.Id, group["carlos"])).Should().ContainSingle(f => f.Id == flashId);
    }

    [Fact]
    public async Task WithoutCoins_TheExtensionRollsBack()
    {
        var group = await SeedAsync();
        var (_, launched) = await LaunchAsync(group, "ana", new FlashOptions());

        var (result, _) = await ExtendAsync(group, "carlos", launched!.Flash.Id);

        result.Should().Be(FlashResult.NotEnoughCoins);
        await using var db = NewContext();
        var flash = await db.FlashEntries.SingleAsync();
        flash.ExtendedByUserId.Should().BeNull();
        flash.ClosesAt.Should().Be(launched.Flash.ClosesAt);
    }

    [Fact]
    public async Task AClosedFlash_CannotBeExtended()
    {
        var group = await SeedAsync();
        var (_, launched) = await LaunchAsync(group, "ana", new FlashOptions());
        Clock.SetLocal(Today.AddDays(1), Morning.AddMinutes(1));

        var (result, _) = await ExtendAsync(group, "bea", launched!.Flash.Id);

        result.Should().Be(FlashResult.Closed);
    }

    [Fact]
    public async Task AnAnonymousFlash_HidesWhoExtendedIt()
    {
        var group = await SeedAsync();
        var (_, launched) = await LaunchAsync(group, "ana", new FlashOptions(Anonymous: true));

        var (_, response) = await ExtendAsync(group, "bea", launched!.Flash.Id);

        response!.Flash.Extended.Should().BeTrue();
        response.Flash.ExtendedBy.Should().BeNull();
    }
}
