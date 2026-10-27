using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.DTOs.Streaks;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Services;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Daily;

// Free duels (§17.15 item 6, rama 47): a 1-vs-1 Deathmatch launched as a flash or actively
// chosen by the selector is a duel. Its two sides get a duel push in place of the normal one,
// and closed duels anyone voted on make a won · drawn · lost record on the member card.
[Collection(PostgresCollection.Name)]
public class DuelTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    // ── Rules ────────────────────────────────────────────────────────────────

    private static Question Deathmatch(params List<int>[] teams) =>
        new() { Type = QuestionType.Deathmatch, Metadata = new QuestionMetadata { Teams = [.. teams] } };

    [Fact]
    public void OnlyTwoTeamsOfOneMemberEach_AreADuel()
    {
        DuelRules.Duelists(Deathmatch([1], [2])).Should().Be((1, 2));
        DuelRules.Duelists(Deathmatch([1, 3], [2])).Should().BeNull();
        DuelRules.Duelists(Deathmatch([1], [2], [3])).Should().BeNull();
        DuelRules.Duelists(new Question { Type = QuestionType.Superlative }).Should().BeNull();
    }

    [Theory]
    [InlineData(3, 1, DuelOutcome.Won)]
    [InlineData(1, 3, DuelOutcome.Lost)]
    [InlineData(2, 2, DuelOutcome.Drawn)]
    [InlineData(0, 0, null)]
    public void TheMostVotedSideWins_EqualVotesDraw_AndNoVotesDontCount(int own, int opponent, DuelOutcome? expected) =>
        DuelRules.Outcome(own, opponent).Should().Be(expected);

    [Theory]
    [InlineData(SelectionKind.Auto, false)]
    [InlineData(SelectionKind.Confirmed, true)]
    [InlineData(SelectionKind.Picked, true)]
    [InlineData(SelectionKind.Created, true)]
    public void ADailyOneVsOne_IsADuelOnlyWhenSomeoneChoseIt(SelectionKind kind, bool expected) =>
        DuelRules.IsDailyDuel(new DailyEntry { SelectionKind = kind }, Deathmatch([1], [2])).Should().Be(expected);

    // ── Push ─────────────────────────────────────────────────────────────────

    private async Task<int> AddDuelQuestionAsync(int groupId, int first, int second) =>
        await AddQuestionAsync(
            groupId,
            QuestionType.Deathmatch,
            "¿Quién ganaría un pulso?",
            configure: q => q.Metadata = new QuestionMetadata { Teams = [[first], [second]] }
        );

    [Fact]
    public async Task AChosenDailyDuel_TellsItsSidesWhoSetItUp()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        var questionId = await AddDuelQuestionAsync(group.Id, group["bea"], group["carlos"]);
        await AddEntryAsync(group.Id, Today, questionId, group["ana"], activated: false, SelectionKind.Picked);
        Clock.SetLocal(Today, DailyTime);

        await TickAsync();

        Notifications.Verify(
            n => n.SendNewQuestionAsync(
                group.Id,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                new DuelPush(group["ana"], "ana", group["bea"], group["carlos"])
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task AnAutoSelectedOneVsOne_IsNotADuel()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        var questionId = await AddDuelQuestionAsync(group.Id, group["bea"], group["carlos"]);
        await AddEntryAsync(group.Id, Today, questionId, group["ana"], activated: false);
        Clock.SetLocal(Today, DailyTime);

        await TickAsync();

        Notifications.Verify(
            n => n.SendNewQuestionAsync(
                group.Id,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>(),
                null
            ),
            Times.Once
        );
    }

    private async Task<int> LaunchFlashDuelAsync(SeededGroup group, string launcher, string first, string second)
    {
        await using var db = NewContext();
        await db.Users.Where(u => u.Id == group[launcher])
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Coins, CoinRewards.FlashQuestionPrice));
        var (result, response) = await NewFlashService(db).LaunchAsync(
            group.Id,
            group[launcher],
            new CreateQuestionDto
            {
                Text = "¿Quién ganaría un pulso?",
                Type = QuestionType.Deathmatch,
                Teams = [[group[first]], [group[second]]],
            }
        );
        result.Should().Be(FlashResult.Success);
        return response!.Flash.Id;
    }

    [Fact]
    public async Task AFlashDuel_ChallengesTheOtherSide()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        Clock.SetLocal(Today, new TimeOnly(10, 0));

        await LaunchFlashDuelAsync(group, "ana", "ana", "bea");

        Notifications.Verify(
            n => n.SendFlashQuestionAsync(
                group.Id,
                group["ana"],
                "Los de siempre",
                "ana",
                It.IsAny<string>(),
                // The launcher is to be guessed (§17.17), so the duel push names nobody either.
                new DuelPush(group["ana"], "ana", group["ana"], group["bea"], Anonymous: true),
                false,
                true
            ),
            Times.Once
        );
    }

    // ── Record ───────────────────────────────────────────────────────────────

    private async Task VoteAsync(int questionId, int voterId, int sideId, bool late = false)
    {
        await using var db = NewContext();
        db.Votes.Add(new Vote { QuestionId = questionId, UserId = voterId, SelectedTargetUserId = sideId, IsLate = late });
        await db.SaveChangesAsync();
    }

    private async Task<MemberStatsDto> StatsAsync(SeededGroup group, string member)
    {
        await using var db = NewContext();
        return (await NewStreakService(db).GetMemberStatsAsync(group.Id, group[member]))!;
    }

    [Fact]
    public async Task ClosedDuels_MakeTheRecord_AndOpenOrUnvotedOnesWait()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos", "dani");
        int ana = group["ana"], bea = group["bea"], carlos = group["carlos"], dani = group["dani"];

        // Day 1: ana beats bea 2–1 (one of them a late vote).
        var won = await AddDuelQuestionAsync(group.Id, ana, bea);
        await AddEntryAsync(group.Id, Today.AddDays(-4), won, carlos, activated: true, SelectionKind.Picked);
        await VoteAsync(won, carlos, ana);
        await VoteAsync(won, dani, ana, late: true);
        await VoteAsync(won, bea, bea);

        // Day 2: auto-selected, so not a duel even with votes.
        var auto = await AddDuelQuestionAsync(group.Id, ana, bea);
        await AddEntryAsync(group.Id, Today.AddDays(-3), auto, carlos, activated: true);
        await VoteAsync(auto, carlos, bea);

        // Day 3: confirmed by the selector, nobody voted: doesn't count.
        var unvoted = await AddDuelQuestionAsync(group.Id, ana, carlos);
        await AddEntryAsync(group.Id, Today.AddDays(-2), unvoted, dani, activated: true, SelectionKind.Confirmed);

        // Day 4: ana and carlos draw 1–1.
        var drawn = await AddDuelQuestionAsync(group.Id, carlos, ana);
        await AddEntryAsync(group.Id, Today.AddDays(-1), drawn, bea, activated: true, SelectionKind.Created);
        await VoteAsync(drawn, bea, carlos);
        await VoteAsync(drawn, dani, ana);

        // Today (still open): would be a loss, but it hasn't closed yet.
        var open = await AddDuelQuestionAsync(group.Id, ana, dani);
        await AddEntryAsync(group.Id, Today, open, bea, activated: true, SelectionKind.Picked);
        await VoteAsync(open, bea, dani);

        Clock.SetLocal(Today, new TimeOnly(10, 0));
        // A flash duel ana loses 0–1, closed 24 h after launch.
        var flashId = await LaunchFlashDuelAsync(group, "bea", "ana", "bea");
        await using (var db = NewContext())
        {
            var flashQuestionId = await db.FlashEntries.Where(f => f.Id == flashId).Select(f => f.QuestionId).SingleAsync();
            await VoteAsync(flashQuestionId, carlos, bea);
        }

        var whileFlashOpen = await StatsAsync(group, "ana");
        (whileFlashOpen.DuelsWon, whileFlashOpen.DuelsDrawn, whileFlashOpen.DuelsLost).Should().Be((1, 1, 0));

        Clock.Advance(FlashService.OpenFor);
        var stats = await StatsAsync(group, "ana");
        (stats.DuelsWon, stats.DuelsDrawn, stats.DuelsLost).Should().Be((1, 1, 1));

        var beaStats = await StatsAsync(group, "bea");
        (beaStats.DuelsWon, beaStats.DuelsDrawn, beaStats.DuelsLost).Should().Be((1, 0, 1));
    }
}
