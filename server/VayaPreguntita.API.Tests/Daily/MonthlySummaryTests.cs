using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Daily;

// Monthly summary (§17.12): built by the tick after the month ends, a few groups at a time,
// announced with the group's activation on the 1st.
[Collection(PostgresCollection.Name)]
public class MonthlySummaryTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly October = new(2026, 10, 1);
    private static readonly DateOnly NovemberFirst = new(2026, 11, 1);

    private async Task AddVoteAsync(int questionId, int userId, int? targetUserId = null, bool isLate = false)
    {
        await using var db = NewContext();
        db.Votes.Add(new Vote { QuestionId = questionId, UserId = userId, SelectedTargetUserId = targetUserId, IsLate = isLate });
        await db.SaveChangesAsync();
    }

    private async Task<List<MonthlySummary>> SummariesAsync()
    {
        await using var db = NewContext();
        return await db.MonthlySummaries.AsNoTracking().ToListAsync();
    }

    // October had two questions; November's first one waits for T.
    private async Task<SeededGroup> SeedOctoberAsync()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        var (_, q1) = await AddActivatedDayAsync(group.Id, October.AddDays(10), group["ana"]);
        var (_, q2) = await AddActivatedDayAsync(group.Id, October.AddDays(11), group["bea"]);
        await AddVoteAsync(q1, group["ana"], group["bea"]);
        await AddVoteAsync(q1, group["bea"], group["ana"]);
        await AddVoteAsync(q2, group["ana"], group["bea"]);
        await AddVoteAsync(q2, group["carlos"], group["bea"], isLate: true);

        var nov1 = await AddQuestionAsync(group.Id, text: "La del 1 de noviembre");
        await AddEntryAsync(group.Id, NovemberFirst, nov1, group["carlos"], activated: false);
        await AddQuestionAsync(group.Id, text: "Para el día 2");
        return group;
    }

    [Fact]
    public async Task OnThe1st_LastMonthIsBuiltAtOnce_AndAnnouncedWithTheActivationAtT()
    {
        var group = await SeedOctoberAsync();

        Clock.SetLocal(NovemberFirst, new TimeOnly(0, 1));
        await TickAsync();

        var summary = (await SummariesAsync()).Should().ContainSingle().Subject;
        summary.Month.Should().Be(October);
        summary.NotifiedAt.Should().BeNull();
        Notifications.Verify(n => n.SendMonthlySummaryAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateOnly>()), Times.Never);

        Clock.SetLocal(NovemberFirst, DailyTime);
        await TickAsync();
        await TickAsync();

        Notifications.Verify(n => n.SendMonthlySummaryAsync(group.Id, "Los de siempre", October), Times.Once);
        (await SummariesAsync()).Should().ContainSingle().Which.NotifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task BuiltAfterTodaysQuestionOpened_ItIsAnnouncedRightAway()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await AddActivatedDayAsync(group.Id, October.AddDays(3), group["ana"]);
        await AddActivatedDayAsync(group.Id, NovemberFirst.AddDays(2), group["bea"]);

        // The server slept through the 1st and wakes on the 3rd, after T.
        Clock.SetLocal(NovemberFirst.AddDays(2), DailyTime.AddHours(1));
        await using (var db = NewContext())
            await NewMonthlySummaryService(db).BuildPendingAsync();

        Notifications.Verify(n => n.SendMonthlySummaryAsync(group.Id, "Los de siempre", October), Times.Once);
    }

    [Fact]
    public async Task GroupsAreBuiltInBatches_AndOnlyIfTheyPlayedThatMonth()
    {
        var played = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            var g = await SeedGroupAsync($"a{i}", $"b{i}");
            await AddActivatedDayAsync(g.Id, October.AddDays(i), g[$"a{i}"]);
            played.Add(g.Id);
        }
        var idle = await SeedGroupAsync("x", "y");
        await AddActivatedDayAsync(idle.Id, October.AddMonths(-1), idle["x"]);

        Clock.SetLocal(NovemberFirst, new TimeOnly(0, 1));
        await using (var db = NewContext())
            await NewMonthlySummaryService(db).BuildPendingAsync(batchSize: 2);
        (await SummariesAsync()).Should().HaveCount(2);

        await using (var db = NewContext())
            await NewMonthlySummaryService(db).BuildPendingAsync(batchSize: 2);
        await using (var db = NewContext())
            await NewMonthlySummaryService(db).BuildPendingAsync(batchSize: 2);

        (await SummariesAsync()).Select(s => s.GroupId).Should().BeEquivalentTo(played);
    }

    [Fact]
    public async Task TheSummary_LeavesLateVotesOut_AndOnlyShowsTheCallersOwnMatch()
    {
        var group = await SeedOctoberAsync();
        Clock.SetLocal(NovemberFirst, new TimeOnly(0, 1));
        await TickAsync();

        await using var db = NewContext();
        var service = NewMonthlySummaryService(db);

        (await service.ListAsync(group.Id)).Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Month = "2026-10", Questions = 2 });

        var forAna = (await service.GetAsync(group.Id, group["ana"], "2026-10"))!;
        forAna.Questions.Should().Be(2);
        forAna.Participation.Should().Be(MonthlySummaryBuilder.Percent(3, 6));
        forAna.Members.Select(m => m.Person.Username).Should().Equal("ana", "bea", "carlos");
        forAna.Members.Single(m => m.Person.Username == "carlos").Answered.Should().Be(0);

        var superlative = forAna.Awards.Single(a => a.Key == MonthlySummaryBuilder.AwardKeys.Superlative);
        superlative.Value.Should().Be(2, "carlos's late vote doesn't count");
        superlative.Winners.Single().Person.Username.Should().Be("bea");

        forAna.MyMatch!.People.Single().Username.Should().Be("bea");
        forAna.MyMatch.Count.Should().Be(3);
        (await service.GetAsync(group.Id, group["carlos"], "2026-10"))!.MyMatch.Should().BeNull();

        (await service.GetAsync(group.Id, group["ana"], "2026-09")).Should().BeNull();
        (await service.GetAsync(group.Id, group["ana"], "octubre")).Should().BeNull();
    }
}
