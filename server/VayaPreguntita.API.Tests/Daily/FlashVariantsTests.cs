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

// Flash variants (rama 50, §17.15 item 6): the anonymous-author, anonymous-votes and
// time-capsule add-ons on top of a flash, combinable and paid in coins, and the +24 h extension
// any member can buy once.
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
    public async Task AnAnonymousAuthor_IsNeverNamed_ButTheVotesStayPublic()
    {
        var group = await SeedAsync();

        var (result, response) = await LaunchAsync(group, "ana", new FlashOptions(AnonymousAuthor: true));

        result.Should().Be(FlashResult.Success);
        response!.Balance.Should().Be(600 - CoinRewards.FlashQuestionPrice - CoinRewards.FlashAnonymousAuthorPrice);
        response.Flash.AnonymousAuthor.Should().BeTrue();
        response.Flash.AnonymousVotes.Should().BeFalse();
        response.Flash.LaunchedBy.Should().BeNull();
        Notifications.Verify(
            n => n.SendFlashQuestionAsync(group.Id, group["ana"], It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), null, true),
            Times.Once
        );

        var flashId = response.Flash.Id;
        var state = await VoteTextAsync(group, "bea", flashId, "Que es majo");
        state.VoterIds.Should().Equal(group["bea"]);
        state.Results!.AnonymousVotes.Should().BeFalse();
        state.Results.FreeTextResponses.Single().Username.Should().Be("bea");

        Clock.SetLocal(Today.AddDays(1), Morning.AddMinutes(1));
        await using var db = NewContext();
        var history = await new QuestionsService(db, TestMapper.Instance, Clock).GetHistoryAsync(group.Id, group["bea"], null);
        var item = history.Items.Single(i => i.FlashId == flashId);
        item.AuthorUsername.Should().BeNull();
        item.AnonymousAuthor.Should().BeTrue();
        (await NewFlashService(db).GetDetailAsync(group.Id, group["bea"], flashId))!.Author.Should().BeNull();
    }

    [Fact]
    public async Task AnonymousVotes_NameNoVoter_ButTheLauncherStaysPublic()
    {
        var group = await SeedAsync();

        var (result, response) = await LaunchAsync(group, "ana", new FlashOptions(AnonymousVotes: true));

        result.Should().Be(FlashResult.Success);
        response!.Balance.Should().Be(600 - CoinRewards.FlashQuestionPrice - CoinRewards.FlashAnonymousVotesPrice);
        response.Flash.AnonymousVotes.Should().BeTrue();
        response.Flash.LaunchedBy!.Username.Should().Be("ana");

        await using (var db = NewContext())
        {
            var reasons = await db.CoinTransactions.Where(c => c.UserId == group["ana"]).Select(c => c.Reason).ToListAsync();
            reasons.Should().BeEquivalentTo([CoinReason.FlashQuestion, CoinReason.FlashAnonymousVotes]);
        }

        var flashId = response.Flash.Id;
        await VoteTextAsync(group, "bea", flashId, "Que es majo");
        var state = await VoteTextAsync(group, "carlos", flashId, "Mejor no digo nada");

        state.VoterIds.Should().BeEmpty("nobody may see who has voted");
        state.VoteCount.Should().Be(2);
        state.Results!.AnonymousVotes.Should().BeTrue();
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
            item.AuthorUsername.Should().Be("ana");
            item.AnonymousAuthor.Should().BeFalse();
            var detail = await NewFlashService(db).GetDetailAsync(group.Id, group["bea"], flashId);
            detail!.Author!.Username.Should().Be("ana");
            detail.FreeTextResponses.Should().OnlyContain(a => a.UserId == 0 || a.UserId == group["bea"]);
        }
    }

    [Fact]
    public async Task AnAnonymousSuperlative_KeepsTheTallies_ButNoVoters()
    {
        var group = await SeedAsync();
        var (_, response) = await LaunchAsync(group, "ana", new FlashOptions(AnonymousVotes: true), QuestionType.Superlative);

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
    public async Task AllAddOns_Combine_AndAreCheckedBeforeCharging()
    {
        var group = await SeedAsync(coins: 200);
        var all = new FlashOptions(AnonymousAuthor: true, AnonymousVotes: true, CapsuleOpensOn: Today.AddDays(1));

        var (poor, _) = await LaunchAsync(group, "ana", all);
        poor.Should().Be(FlashResult.NotEnoughCoins);
        (await CoinsAsync(group, "ana")).Should().Be(200);

        await using (var db = NewContext())
            await db.Users.Where(u => u.Id == group["ana"]).ExecuteUpdateAsync(s => s.SetProperty(u => u.Coins, 250));
        var (result, response) = await LaunchAsync(group, "ana", all);

        result.Should().Be(FlashResult.Success);
        response!.Balance.Should().Be(0);
        response.Flash.AnonymousAuthor.Should().BeTrue();
        response.Flash.AnonymousVotes.Should().BeTrue();
        response.Flash.Sealed.Should().BeTrue();
    }

    // ── Extension ────────────────────────────────────────────────────────────

    [Fact]
    public async Task TheLauncher_CanExtendTheirOpenFlash_Once()
    {
        var group = await SeedAsync();
        var (_, launched) = await LaunchAsync(group, "ana", new FlashOptions());
        var flashId = launched!.Flash.Id;
        launched.Flash.IsMine.Should().BeTrue();

        var (result, response) = await ExtendAsync(group, "ana", flashId);

        result.Should().Be(FlashResult.Success);
        response!.Balance.Should().Be(600 - CoinRewards.FlashQuestionPrice - CoinRewards.FlashExtensionPrice);
        response.Flash.ClosesAt.Should().Be(launched.Flash.ClosesAt + TimeSpan.FromHours(24));
        response.Flash.Extended.Should().BeTrue();
        response.Flash.ExtendedBy!.Username.Should().Be("ana");

        var (again, _) = await ExtendAsync(group, "ana", flashId);
        again.Should().Be(FlashResult.AlreadyExtended);
        (await CoinsAsync(group, "ana")).Should().Be(600 - CoinRewards.FlashQuestionPrice - CoinRewards.FlashExtensionPrice);

        await using var db = NewContext();
        var charge = await db.CoinTransactions.SingleAsync(c => c.UserId == group["ana"] && c.Reason == CoinReason.FlashExtension);
        charge.Reason.Should().Be(CoinReason.FlashExtension);
        charge.RefId.Should().Be(flashId);

        // Still open the day after, when it would have closed.
        Clock.SetLocal(Today.AddDays(1), Morning.AddMinutes(1));
        (await NewFlashService(db).GetOpenAsync(group.Id, group["carlos"])).Should().ContainSingle(f => f.Id == flashId);
    }

    [Fact]
    public async Task AnotherMember_CannotExtendSomeoneElsesFlash()
    {
        var group = await SeedAsync();
        var (_, launched) = await LaunchAsync(group, "ana", new FlashOptions(AnonymousAuthor: true));

        var (result, _) = await ExtendAsync(group, "bea", launched!.Flash.Id);

        result.Should().Be(FlashResult.NotLauncher);
        (await CoinsAsync(group, "bea")).Should().Be(600);
        await using var db = NewContext();
        (await db.FlashEntries.SingleAsync()).ExtendedByUserId.Should().BeNull();
        // Only its author is told an anonymous flash is theirs.
        (await NewFlashService(db).GetOpenAsync(group.Id, group["bea"])).Single().IsMine.Should().BeFalse();
    }

    [Fact]
    public async Task WithoutCoins_TheExtensionRollsBack()
    {
        var group = await SeedAsync();
        var (_, launched) = await LaunchAsync(group, "ana", new FlashOptions());
        await using (var drain = NewContext())
        {
            var ana = await drain.Users.SingleAsync(u => u.Id == group["ana"]);
            ana.Coins = 0;
            await drain.SaveChangesAsync();
        }

        var (result, _) = await ExtendAsync(group, "ana", launched!.Flash.Id);

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

        var (result, _) = await ExtendAsync(group, "ana", launched!.Flash.Id);

        result.Should().Be(FlashResult.Closed);
    }

    [Fact]
    public async Task AnAnonymousFlash_HidesWhoExtendedIt()
    {
        var group = await SeedAsync();
        var (_, launched) = await LaunchAsync(group, "ana", new FlashOptions(AnonymousAuthor: true));

        var (_, response) = await ExtendAsync(group, "ana", launched!.Flash.Id);

        response!.Flash.Extended.Should().BeTrue();
        response.Flash.ExtendedBy.Should().BeNull();
    }
}
