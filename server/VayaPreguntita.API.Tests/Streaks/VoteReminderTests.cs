using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Streaks;

// Vote reminder (rama 61): before the cycle closes, a push to the members who haven't voted
// the open question and have no streak in danger (those get the "streak in danger" push).
[Collection(PostgresCollection.Name)]
public class VoteReminderTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Day1 = new(2026, 10, 5);

    // Day 1 is open, bea has voted it and ana hasn't; one hour is left.
    private async Task<SeededGroup> SeedOpenDayAsync()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await AddActivatedDayAsync(group.Id, Day1, group["bea"]);
        await VoteAsync(group, "bea", Day1);
        await SeenAsync(group, "ana", Day1);
        Clock.SetLocal(Day1.AddDays(1), DailyTime.AddHours(-1));
        return group;
    }

    private async Task VoteAsync(SeededGroup group, string username, DateOnly date)
    {
        Clock.SetLocal(date, DailyTime.AddHours(1));
        await using var db = NewContext();
        var target = username == "ana" ? "bea" : "ana";
        var (result, _) = await NewDailyService(db)
            .VoteAsync(group.Id, group[username], new() { SelectedTargetUserId = group[target] });
        result.Should().Be(VoteResult.Success);
    }

    // The member last had the group open on that day.
    private async Task SeenAsync(SeededGroup group, string username, DateOnly date)
    {
        Clock.SetLocal(date, DailyTime.AddHours(1));
        await using var db = NewContext();
        var member = await db.GroupMembers.SingleAsync(m =>
            m.GroupId == group.Id && m.UserId == group[username]
        );
        member.LastSeenAt = Clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync();
    }

    private async Task RemindAsync()
    {
        await using var db = NewContext();
        await NewStreakService(db).SendPreCloseRemindersAsync();
    }

    private void VerifyReminders(SeededGroup group, string username, Times times) =>
        Notifications.Verify(
            n =>
                n.SendVoteReminderAsync(
                    group.Id,
                    group[username],
                    It.IsAny<string>(),
                    DailyTime,
                    It.IsAny<int>(),
                    It.IsAny<int>()
                ),
            times
        );

    [Fact]
    public async Task WhoeverHasNotVoted_IsRemindedOnce_WithHowManyHave()
    {
        var group = await SeedOpenDayAsync();

        await RemindAsync();
        await RemindAsync();

        Notifications.Verify(
            n =>
                n.SendVoteReminderAsync(
                    group.Id,
                    group["ana"],
                    It.IsAny<string>(),
                    DailyTime,
                    1,
                    2
                ),
            Times.Once()
        );
        VerifyReminders(group, "bea", Times.Never());
    }

    [Fact]
    public async Task NothingIsSent_BeforeTheMembersLeadTime()
    {
        var group = await SeedOpenDayAsync();
        Clock.SetLocal(Day1.AddDays(1), DailyTime.AddHours(-5)); // default lead: 3 h

        await RemindAsync();

        VerifyReminders(group, "ana", Times.Never());
    }

    [Fact]
    public async Task TurningThePreferenceOff_OrMutingTheGroup_SilencesIt()
    {
        var group = await SeedOpenDayAsync();
        var other = await SeedGroupAsync("carla", "dani");
        await AddActivatedDayAsync(other.Id, Day1, other["dani"]);
        await using (var db = NewContext())
        {
            db.NotificationPreferences.Add(
                new NotificationPreferences { UserId = group["ana"], VoteReminder = false }
            );
            var carla = await db.GroupMembers.SingleAsync(m =>
                m.GroupId == other.Id && m.UserId == other["carla"]
            );
            carla.NotificationsMuted = true;
            carla.LastSeenAt = Clock.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync();
        }

        await RemindAsync();

        VerifyReminders(group, "ana", Times.Never());
        VerifyReminders(other, "carla", Times.Never());
    }

    [Fact]
    public async Task AMemberAwayForOverAWeek_IsLeftAlone()
    {
        var group = await SeedOpenDayAsync();
        await SeenAsync(group, "ana", Day1.AddDays(-8));
        Clock.SetLocal(Day1.AddDays(1), DailyTime.AddHours(-1));

        await RemindAsync();

        VerifyReminders(group, "ana", Times.Never());
    }

    [Fact]
    public async Task AStreakInDanger_GetsItsOwnPush_NotTheReminder()
    {
        var group = await SeedGroupAsync("ana", "bea");
        for (var day = 0; day < 3; day++)
        {
            await AddActivatedDayAsync(group.Id, Day1.AddDays(day), group["bea"]);
            await VoteAsync(group, "ana", Day1.AddDays(day));
        }
        await AddActivatedDayAsync(group.Id, Day1.AddDays(3), group["bea"]);
        await SeenAsync(group, "ana", Day1.AddDays(3));
        Clock.SetLocal(Day1.AddDays(4), DailyTime.AddHours(-1));

        await RemindAsync();

        Notifications.Verify(n =>
            n.SendStreakDangerAsync(
                group.Id,
                group["ana"],
                It.IsAny<string>(),
                3,
                DailyTime,
                It.IsAny<bool>()
            )
        );
        VerifyReminders(group, "ana", Times.Never());
    }

    [Fact]
    public async Task WithTheDangerPushOff_AStreakHolderStillGetsTheReminder()
    {
        var group = await SeedGroupAsync("ana", "bea");
        for (var day = 0; day < 3; day++)
        {
            await AddActivatedDayAsync(group.Id, Day1.AddDays(day), group["bea"]);
            await VoteAsync(group, "ana", Day1.AddDays(day));
        }
        await AddActivatedDayAsync(group.Id, Day1.AddDays(3), group["bea"]);
        await SeenAsync(group, "ana", Day1.AddDays(3));
        await using (var db = NewContext())
        {
            db.NotificationPreferences.Add(
                new NotificationPreferences { UserId = group["ana"], StreakDanger = false }
            );
            await db.SaveChangesAsync();
        }
        Clock.SetLocal(Day1.AddDays(4), DailyTime.AddHours(-1));

        await RemindAsync();

        VerifyReminders(group, "ana", Times.Once());
    }
}
