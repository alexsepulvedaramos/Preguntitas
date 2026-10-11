using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.DTOs.Streaks;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Streaks;

// Per-group voting streaks (rama 19): the unit is the daily cycle; a streak only breaks
// when an activated question closes without the member's vote, and cycles with no active
// question leave it frozen.
[Collection(PostgresCollection.Name)]
public class StreakTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Day1 = new(2026, 10, 5);

    // Opens the day's question at T and has ana vote for bea an hour later.
    private async Task<StreakUpdateDto> OpenAndVoteAsync(SeededGroup group, DateOnly date)
    {
        await AddActivatedDayAsync(group.Id, date, group["bea"]);
        Clock.SetLocal(date, DailyTime.AddHours(1));
        await using var db = NewContext();
        var (_, response) = await NewDailyService(db).VoteAsync(
            group.Id,
            group["ana"],
            new() { SelectedTargetUserId = group["bea"] }
        );
        return response!.Streak;
    }

    private async Task<MyStreakDto> AnasStreakAsync(SeededGroup group)
    {
        await using var db = NewContext();
        return await NewStreakService(db).GetMyStreakAsync(group.Id, group["ana"]);
    }

    [Fact]
    public async Task VotingOnConsecutiveCycles_GrowsTheStreakUpToTheFirstFrame()
    {
        var group = await SeedGroupAsync("ana", "bea");

        var day1 = await OpenAndVoteAsync(group, Day1);
        var day2 = await OpenAndVoteAsync(group, Day1.AddDays(1));
        var day3 = await OpenAndVoteAsync(group, Day1.AddDays(2));

        (day1.Previous, day1.Current).Should().Be((0, 1));
        day1.TierKey.Should().BeNull("there is no frame before day 3");
        day1.DaysToNextTier.Should().Be(2);
        (day2.Previous, day2.Current).Should().Be((1, 2));
        (day3.Previous, day3.Current).Should().Be((2, 3));
        day3.IsTierUp.Should().BeTrue();
        day3.TierKey.Should().Be("wood");
        day3.TitleUnlocked.Should().BeTrue();
    }

    [Fact]
    public async Task MissingAnActiveQuestion_BreaksTheStreak()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await OpenAndVoteAsync(group, Day1);
        await OpenAndVoteAsync(group, Day1.AddDays(1));
        await AddActivatedDayAsync(group.Id, Day1.AddDays(2), group["bea"]);

        // While the missed question is still open the streak is alive: it can still be saved.
        (await AnasStreakAsync(group)).Current.Should().Be(2);

        var day4 = Day1.AddDays(3);
        await AddActivatedDayAsync(group.Id, day4, group["bea"]);
        var beforeVoting = await AnasStreakAsync(group);
        beforeVoting.Current.Should().Be(0);
        beforeVoting.LostStreak.Should().Be(2);
        beforeVoting.Best.Should().Be(2);
    }

    [Fact]
    public async Task AfterABreak_TheNextVoteStartsAgainFromOne()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await OpenAndVoteAsync(group, Day1);
        await OpenAndVoteAsync(group, Day1.AddDays(1));
        await AddActivatedDayAsync(group.Id, Day1.AddDays(2), group["bea"]);

        var update = await OpenAndVoteAsync(group, Day1.AddDays(3));

        (update.Previous, update.Current, update.Best).Should().Be((0, 1, 2));
    }

    [Fact]
    public async Task DaysWithoutAnActiveQuestion_FreezeTheStreak()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await OpenAndVoteAsync(group, Day1);
        await OpenAndVoteAsync(group, Day1.AddDays(1));

        // Three days with no question at all (e.g. the group dropped below 2 members).
        var update = await OpenAndVoteAsync(group, Day1.AddDays(5));

        (update.Previous, update.Current).Should().Be((2, 3));
    }

    [Fact]
    public async Task Rejoining_RebuildsTheStreakFromVoteHistory()
    {
        var group = await SeedGroupAsync("ana", "bea");
        // Voted days 1–3, missed day 4, voted day 5; day 6 is open and not voted yet.
        var questions = new List<int>();
        for (var day = 0; day < 6; day++)
            questions.Add((await AddActivatedDayAsync(group.Id, Day1.AddDays(day), group["bea"])).QuestionId);

        await using (var setup = NewContext())
        {
            foreach (var day in new[] { 0, 1, 2, 4 })
                setup.Votes.Add(new Vote { QuestionId = questions[day], UserId = group["ana"], SelectedTargetUserId = group["bea"] });
            await setup.SaveChangesAsync();
        }
        Clock.SetLocal(Day1.AddDays(5), DailyTime.AddHours(1));

        await using var db = NewContext();
        await NewStreakService(db).RecalculateMemberAsync(group.Id, group["ana"]);

        var member = await db.GroupMembers.AsNoTracking().SingleAsync(m => m.UserId == group["ana"]);
        member.CurrentStreak.Should().Be(1, "the open day can still be voted, so it doesn't break it");
        member.BestStreak.Should().Be(3);
        (await AnasStreakAsync(group)).Current.Should().Be(1);
        (await db.Users.SingleAsync(u => u.Id == group["ana"])).HighestStreakEver.Should().Be(3);
    }
}
