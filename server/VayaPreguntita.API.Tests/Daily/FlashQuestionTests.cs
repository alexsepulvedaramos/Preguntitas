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

// Flash question (§17.11): bought for 500 coins, launched at once, open 24 h next to the
// daily question, one per member and group per local day, and worth nothing for streak or
// coins. Closed flashes show up in the history, gated like any other question.
[Collection(PostgresCollection.Name)]
public class FlashQuestionTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Today = new(2026, 10, 5);
    private static readonly TimeOnly Morning = new(10, 0);

    // Today's daily question is open; ana and bea can afford a flash.
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

    private static CreateQuestionDto Question(string text = "¿Quién se comería la última croqueta?") =>
        new() { Text = text, Type = QuestionType.Superlative };

    private async Task<(FlashResult Result, LaunchFlashResponseDto? Response)> LaunchAsync(
        SeededGroup group,
        string launcher,
        string text = "¿Quién se comería la última croqueta?"
    )
    {
        await using var db = NewContext();
        return await NewFlashService(db).LaunchAsync(group.Id, group[launcher], Question(text));
    }

    private async Task<(FlashResult Result, FlashStateDto? State)> VoteAsync(
        SeededGroup group,
        string voter,
        int flashId,
        string target
    )
    {
        await using var db = NewContext();
        return await NewFlashService(db)
            .VoteAsync(group.Id, group[voter], flashId, new() { SelectedTargetUserId = group[target] });
    }

    private async Task<DailyStatusDto> StatusAsync(SeededGroup group, string viewer)
    {
        await using var db = NewContext();
        return await NewDailyService(db).GetCurrentStatusAsync(group.Id, group[viewer]);
    }

    [Fact]
    public async Task Launching_ChargesTheCoins_OpensItFor24Hours_AndTellsEveryoneElse()
    {
        var group = await SeedAsync();

        var (result, response) = await LaunchAsync(group, "ana");

        result.Should().Be(FlashResult.Success);
        response!.Balance.Should().Be(100);
        response.Flash.LaunchedBy.Username.Should().Be("ana");
        (response.Flash.ClosesAt - response.Flash.ActivatedAt).Should().Be(TimeSpan.FromHours(24));

        await using var db = NewContext();
        var charge = await db.CoinTransactions.SingleAsync(c => c.UserId == group["ana"]);
        charge.Amount.Should().Be(-CoinRewards.FlashQuestionPrice);
        charge.Reason.Should().Be(CoinReason.FlashQuestion);
        charge.RefId.Should().Be(response.Flash.Id);
        var question = await db.Questions.SingleAsync(q => q.Id == response.Flash.Question.Id);
        question.IsUsed.Should().BeTrue("a flash question never enters the pool");
        question.CreatorId.Should().Be(group["ana"]);
        Notifications.Verify(
            n => n.SendFlashQuestionAsync(group.Id, group["ana"], "Los de siempre", "ana", It.IsAny<string>()),
            Times.Once
        );
    }

    [Fact]
    public async Task WithoutEnoughCoins_NothingIsLaunchedOrCharged()
    {
        var group = await SeedAsync(coins: 499);

        var (result, _) = await LaunchAsync(group, "ana");

        result.Should().Be(FlashResult.NotEnoughCoins);
        await using var db = NewContext();
        (await db.FlashEntries.AnyAsync()).Should().BeFalse();
        (await db.Questions.AnyAsync(q => q.CreatorId == group["ana"])).Should().BeFalse();
        (await db.CoinTransactions.AnyAsync()).Should().BeFalse();
        (await db.Users.SingleAsync(u => u.Id == group["ana"])).Coins.Should().Be(499);
    }

    [Fact]
    public async Task EachMember_CanLaunchOnePerDay_AndSeveralStayOpenTogether()
    {
        var group = await SeedAsync(coins: 1500);

        var first = await LaunchAsync(group, "ana", "Primera");
        var again = await LaunchAsync(group, "ana", "Otra más");
        Clock.SetLocal(Today, Morning.AddHours(1));
        var bea = await LaunchAsync(group, "bea", "La de bea");

        first.Result.Should().Be(FlashResult.Success);
        again.Result.Should().Be(FlashResult.DailyLimitReached);
        bea.Result.Should().Be(FlashResult.Success);

        var flashes = (await StatusAsync(group, "carlos")).Flashes;
        flashes.Select(f => f.Question.Text).Should().Equal("Primera", "La de bea");
        flashes.Should().OnlyContain(f => !f.UserHasVoted && f.Results == null);

        // A new local day gives ana a new one, while yesterday's is still open.
        Clock.SetLocal(Today.AddDays(1), Morning.AddMinutes(-30));
        (await LaunchAsync(group, "ana", "Segunda")).Result.Should().Be(FlashResult.Success);
        (await StatusAsync(group, "carlos")).Flashes.Should().HaveCount(3);

        // 24 h after launch it closes.
        Clock.SetLocal(Today.AddDays(1), Morning);
        (await StatusAsync(group, "carlos")).Flashes.Select(f => f.Question.Text)
            .Should().Equal("La de bea", "Segunda");
    }

    [Fact]
    public async Task AGroupThatHasNotStarted_CannotLaunchOne()
    {
        var solo = await SeedGroupAsync("zoe");

        await using var db = NewContext();
        var (result, _) = await NewFlashService(db).LaunchAsync(solo.Id, solo["zoe"], Question());

        result.Should().Be(FlashResult.GroupNotStarted);
    }

    [Fact]
    public async Task Voting_ShowsTheResults_ButEarnsNothing()
    {
        var group = await SeedAsync();
        var (_, launched) = await LaunchAsync(group, "ana");

        var (result, state) = await VoteAsync(group, "carlos", launched!.Flash.Id, "bea");
        var again = await VoteAsync(group, "carlos", launched.Flash.Id, "ana");

        result.Should().Be(FlashResult.Success);
        state!.UserHasVoted.Should().BeTrue();
        state.Results!.TotalVotes.Should().Be(1);
        state.VoterIds.Should().Equal(group["carlos"]);
        again.Result.Should().Be(FlashResult.AlreadyVoted);

        await using var db = NewContext();
        (await db.CoinTransactions.AnyAsync(c => c.UserId == group["carlos"])).Should().BeFalse();
        var carlos = await db.GroupMembers.SingleAsync(m => m.GroupId == group.Id && m.UserId == group["carlos"]);
        carlos.CurrentStreak.Should().Be(0, "a flash vote doesn't count for the streak");
        (await db.Votes.SingleAsync(v => v.UserId == group["carlos"])).IsLate.Should().BeFalse();
        Notifications.Verify(
            n => n.SendUserVotedAsync(It.IsAny<int>(), group["carlos"], It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task OnceClosed_ItGoesToTheHistory_GatedAndVotableLate()
    {
        var group = await SeedAsync();
        var (_, launched) = await LaunchAsync(group, "ana");
        var flashId = launched!.Flash.Id;
        await VoteAsync(group, "bea", flashId, "carlos");

        // While open it isn't in the history yet, and its detail can't be voted from there.
        await using (var db = NewContext())
        {
            var history = await new QuestionsService(db, TestMapper.Instance, Clock).GetHistoryAsync(group.Id, group["carlos"], null);
            history.Items.Should().NotContain(i => i.FlashId != null);
            var open = await NewFlashService(db).GetDetailAsync(group.Id, group["carlos"], flashId);
            open!.Locked.Should().BeTrue();
            open.LateVoteQuestion.Should().BeNull();
        }

        Clock.SetLocal(Today.AddDays(1), Morning.AddMinutes(1));

        await using (var db = NewContext())
        {
            var history = await new QuestionsService(db, TestMapper.Instance, Clock).GetHistoryAsync(group.Id, group["carlos"], null);
            var item = history.Items.Single(i => i.FlashId == flashId);
            item.Date.Should().Be(Today);
            item.AuthorUsername.Should().Be("ana");
            item.TotalVotes.Should().Be(1);

            var locked = await NewFlashService(db).GetDetailAsync(group.Id, group["carlos"], flashId);
            locked!.Locked.Should().BeTrue();
            locked.Results.Should().BeEmpty();
            locked.Author!.Username.Should().Be("ana");
            locked.LateVoteQuestion.Should().NotBeNull();
        }

        var (late, state) = await VoteAsync(group, "carlos", flashId, "bea");
        late.Should().Be(FlashResult.Success);
        state.Should().BeNull();

        await using (var db = NewContext())
        {
            (await db.Votes.SingleAsync(v => v.UserId == group["carlos"])).IsLate.Should().BeTrue();
            var unlocked = await NewFlashService(db).GetDetailAsync(group.Id, group["carlos"], flashId);
            unlocked!.Locked.Should().BeFalse();
            unlocked.TotalVotes.Should().Be(2);
            (await StatusAsync(group, "carlos")).Flashes.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task TheHistory_SortsFlashesAmongTheDays_AndPagesWithoutRepeats()
    {
        var group = await SeedGroupAsync("ana", "bea");
        var days = Enumerable.Range(0, 4).Select(i => new DateOnly(2026, 10, 2).AddDays(i)).ToList();
        foreach (var day in days)
            await AddActivatedDayAsync(group.Id, day, group["bea"]);
        await using (var db = NewContext())
            await db.Users.Where(u => u.Id == group["ana"]).ExecuteUpdateAsync(s => s.SetProperty(u => u.Coins, 2000));

        // Flashes launched on 2/10 and 3/10 in the morning, before that day's daily question.
        foreach (var day in days.Take(2))
        {
            Clock.SetLocal(day, Morning);
            (await LaunchAsync(group, "ana", $"Flash del {day:dd}")).Result.Should().Be(FlashResult.Success);
        }
        Clock.SetLocal(days[^1], DailyTime.AddHours(1));

        await using var read = NewContext();
        var service = new QuestionsService(read, TestMapper.Instance, Clock);
        var first = await service.GetHistoryAsync(group.Id, group["ana"], null, pageSize: 2);
        var second = await service.GetHistoryAsync(group.Id, group["ana"], first.Items[^1].Date, pageSize: 2);

        first.Items.Select(i => i.QuestionText).Should().Equal("Pregunta del 04/10", "Pregunta del 03/10", "Flash del 03");
        first.HasMore.Should().BeTrue();
        second.Items.Select(i => i.QuestionText).Should().Equal("Pregunta del 02/10", "Flash del 02");
        second.HasMore.Should().BeFalse();
    }
}
