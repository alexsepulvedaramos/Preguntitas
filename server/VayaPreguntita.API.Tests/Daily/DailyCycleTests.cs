using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Daily;

// The background tick (DailyCycleRunner) at fixed instants: tomorrow's question is
// preselected once T passes, today's activates exactly at T (§4.1–§4.3), and there is
// always a question (§4.5, §6.4).
[Collection(PostgresCollection.Name)]
public class DailyCycleTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Today = new(2026, 10, 5);
    private static readonly DateOnly Tomorrow = Today.AddDays(1);

    [Fact]
    public async Task BeforeTheDailyTime_NothingIsPreselected()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        await AddQuestionAsync(group.Id);
        Clock.SetLocal(Today, DailyTime.AddMinutes(-1));

        await TickAsync();

        await using var db = NewContext();
        (await db.DailyEntries.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task AGroupWithASingleMember_NeverStartsACycle()
    {
        var group = await SeedGroupAsync("ana");
        await AddQuestionAsync(group.Id);
        Clock.SetLocal(Today, DailyTime);

        await TickAsync();

        await using var db = NewContext();
        (await db.DailyEntries.AnyAsync()).Should().BeFalse();
        Notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AtTheDailyTime_TomorrowsQuestionIsPreselectedButNotActivated()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        var questionId = await AddQuestionAsync(group.Id);
        Clock.SetLocal(Today, DailyTime);

        await TickAsync();

        await using var db = NewContext();
        var entry = await db.DailyEntries.Include(d => d.Question).SingleAsync();
        entry.Date.Should().Be(Tomorrow);
        entry.QuestionId.Should().Be(questionId);
        entry.IsAutoSelected.Should().BeTrue();
        entry.ActivatedAt.Should().BeNull();
        entry.Question.IsUsed.Should().BeTrue();
        entry.Question.DateActivated.Should().BeNull();
        // 2026-10-06 is day 5 since the group was created: 5 mod 3 → third member to join.
        entry.SelectorUserId.Should().Be(group["carlos"]);
        Notifications.Verify(
            n => n.SendSelectorTurnAsync(group.Id, group["carlos"], "Los de siempre", DailyTime),
            Times.Once
        );
    }

    [Fact]
    public async Task RunningTheTickAgain_DoesNotPreselectTwice()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await AddQuestionAsync(group.Id, text: "Primera");
        await AddQuestionAsync(group.Id, text: "Segunda");
        Clock.SetLocal(Today, DailyTime);

        await TickAsync();
        Clock.Advance(TimeSpan.FromMinutes(1));
        await TickAsync();

        await using var db = NewContext();
        (await db.DailyEntries.CountAsync()).Should().Be(1);
        (await db.Questions.CountAsync(q => !q.IsUsed)).Should().Be(1);
        Notifications.Verify(
            n => n.SendSelectorTurnAsync(group.Id, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TimeOnly>()),
            Times.Once
        );
    }

    [Fact]
    public async Task TheDailyTimeIsSacred_TodaysQuestionWaitsUntilT()
    {
        var group = await SeedGroupAsync("ana", "bea");
        var questionId = await AddQuestionAsync(group.Id);
        await AddEntryAsync(group.Id, Today, questionId, group["ana"], activated: false);
        Clock.SetLocal(Today, DailyTime.AddMinutes(-1));

        await TickAsync();

        await using var db = NewContext();
        (await db.DailyEntries.SingleAsync()).ActivatedAt.Should().BeNull();
        Notifications.Verify(
            n => n.SendNewQuestionAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyDictionary<string, string>>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task AtTheDailyTime_TodaysQuestionActivatesAndTomorrowsIsQueued()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        var todays = await AddQuestionAsync(group.Id, text: "¿Quién sobreviviría menos en una isla desierta?");
        await AddEntryAsync(group.Id, Today, todays, group["bea"], activated: false);
        var next = await AddQuestionAsync(group.Id);
        Clock.SetLocal(Today, DailyTime);

        await TickAsync();

        await using var db = NewContext();
        var entries = await db.DailyEntries.Include(d => d.Question).OrderBy(d => d.Date).ToListAsync();
        entries.Select(e => e.Date).Should().Equal(Today, Tomorrow);

        entries[0].ActivatedAt.Should().Be(Clock.UtcNow);
        entries[0].Question.DateActivated.Should().Be(Clock.UtcNow);
        entries[1].QuestionId.Should().Be(next);
        entries[1].ActivatedAt.Should().BeNull();

        Notifications.Verify(
            n => n.SendNewQuestionAsync(
                group.Id,
                "Los de siempre",
                "¿Quién sobreviviría menos en una isla desierta?",
                It.IsAny<IReadOnlyDictionary<string, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task TheSelectorRotatesThroughTheMembersInJoinOrder()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        for (var i = 0; i < 4; i++)
            await AddQuestionAsync(group.Id, text: $"Pregunta {i}");

        // Four T's in a row: each one activates that day's question and queues the next.
        for (var day = 0; day < 4; day++)
        {
            Clock.SetLocal(Today.AddDays(day), DailyTime);
            await TickAsync();
        }

        await using var db = NewContext();
        var entries = await db.DailyEntries.OrderBy(d => d.Date).ToListAsync();
        entries.Select(e => e.Date).Should().Equal(Enumerable.Range(1, 4).Select(i => Today.AddDays(i)));
        entries
            .Select(e => e.SelectorUserId)
            .Should()
            .Equal(group["carlos"], group["ana"], group["bea"], group["carlos"]);
        entries.Select(e => e.ActivatedAt != null).Should().Equal(true, true, true, false);
    }

    [Fact]
    public async Task AnUnusedPoolQuestionIsPreferredOverThePacks()
    {
        var group = await SeedGroupAsync("ana", "bea");
        var pack = await AddPackAsync("Base");
        await AddTemplateAsync(pack, "Plantilla del pack");
        var poolQuestion = await AddQuestionAsync(group.Id, creatorId: group["bea"]);
        Clock.SetLocal(Today, DailyTime);

        await TickAsync();

        await using var db = NewContext();
        (await db.DailyEntries.SingleAsync()).QuestionId.Should().Be(poolQuestion);
        (await db.Questions.CountAsync()).Should().Be(1, "no pack template should have been cloned");
    }

    [Fact]
    public async Task WithAnEmptyPool_ANeverUsedTemplateFromAnEnabledPackIsCloned()
    {
        var group = await SeedGroupAsync("ana", "bea");
        var basePack = await AddPackAsync("Base");
        var alreadyAsked = await AddTemplateAsync(basePack, "Ya salió");
        await AddTemplateAsync(basePack, "Retirada", retired: true);
        var fresh = await AddTemplateAsync(basePack, "Nueva");
        var disabledForGroup = await AddPackAsync("Picante");
        await AddTemplateAsync(disabledForGroup, "De un pack que el grupo desactivó");
        var inactive = await AddPackAsync("Archivado", activeByDefault: false);
        await AddTemplateAsync(inactive, "De un pack inactivo");

        await using (var setup = NewContext())
        {
            setup.GroupDisabledPacks.Add(new GroupDisabledPack { GroupId = group.Id, PackId = disabledForGroup });
            await setup.SaveChangesAsync();
        }
        await AddQuestionAsync(
            group.Id,
            isUsed: true,
            configure: q =>
            {
                q.TemplateId = alreadyAsked;
                q.Source = QuestionSource.Pack;
            }
        );
        Clock.SetLocal(Today, DailyTime);

        await TickAsync();

        await using var db = NewContext();
        var question = (await db.DailyEntries.Include(d => d.Question).SingleAsync()).Question;
        question.TemplateId.Should().Be(fresh);
        question.Text.Should().Be("Nueva");
        question.Source.Should().Be(QuestionSource.Pack);
        question.CreatorId.Should().BeNull();
        question.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task OnceEveryTemplateHasBeenAsked_OneIsReusedSoThereIsAlwaysAQuestion()
    {
        var group = await SeedGroupAsync("ana", "bea");
        var pack = await AddPackAsync("Base");
        var onlyTemplate = await AddTemplateAsync(pack, "La única");
        await AddQuestionAsync(
            group.Id,
            text: "La única",
            isUsed: true,
            configure: q =>
            {
                q.TemplateId = onlyTemplate;
                q.Source = QuestionSource.Pack;
            }
        );
        Clock.SetLocal(Today, DailyTime);

        await TickAsync();

        await using var db = NewContext();
        var entry = await db.DailyEntries.Include(d => d.Question).SingleAsync();
        entry.Question.TemplateId.Should().Be(onlyTemplate);
        (await db.Questions.CountAsync()).Should().Be(2, "the template is cloned again, not the old question reused");
    }

    [Fact]
    public async Task ASelectorWhoHasntChosenAfterThreeHours_IsRemindedOnce()
    {
        var group = await SeedGroupAsync("ana", "bea");
        var autoPick = await AddQuestionAsync(group.Id);
        // Preselected automatically today at T for tomorrow.
        await AddEntryAsync(group.Id, Tomorrow, autoPick, group["bea"], activated: false);

        Clock.SetLocal(Today, new TimeOnly(23, 59));
        await TickAsync();
        Notifications.Verify(
            n => n.SendSelectorTurnAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TimeOnly>()),
            Times.Never
        );

        Clock.SetLocal(Tomorrow, TimeOnly.MinValue);
        await TickAsync();
        Clock.Advance(TimeSpan.FromMinutes(1));
        await TickAsync();

        Notifications.Verify(
            n => n.SendSelectorTurnAsync(group.Id, group["bea"], "Los de siempre", DailyTime),
            Times.Once
        );
    }

    [Fact]
    public async Task ASelectorWhoAlreadyChose_IsNotReminded()
    {
        var group = await SeedGroupAsync("ana", "bea");
        var chosen = await AddQuestionAsync(group.Id);
        await AddEntryAsync(group.Id, Tomorrow, chosen, group["bea"], activated: false, autoSelected: false);

        Clock.SetLocal(Tomorrow, new TimeOnly(10, 0));
        await TickAsync();

        Notifications.Verify(
            n => n.SendSelectorTurnAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<TimeOnly>()),
            Times.Never
        );
    }

    [Fact]
    public async Task WhenTheSecondMemberJoins_TheFirstQuestionOpensRightAwayAndClosesAtTheNextT()
    {
        var group = await SeedGroupAsync("ana");
        await AddQuestionAsync(group.Id, text: "Primera");
        await AddQuestionAsync(group.Id, text: "Segunda");
        int beaId;
        string invitationCode;
        await using (var setup = NewContext())
        {
            var bea = new User { Username = "bea", Email = "bea@test.dev" };
            setup.Users.Add(bea);
            await setup.SaveChangesAsync();
            beaId = bea.Id;
            invitationCode = (await setup.Groups.SingleAsync()).InvitationCode;
        }
        Clock.SetLocal(Today, new TimeOnly(15, 0));

        await using var db = NewContext();
        var (found, alreadyMember) = await NewGroupsService(db).JoinGroupAsync(beaId, invitationCode);

        found.Should().BeTrue();
        alreadyMember.Should().BeFalse();
        var entries = await db.DailyEntries.AsNoTracking().OrderBy(d => d.Date).ToListAsync();
        entries.Should().HaveCount(2);
        // Bootstrap exception (§4.1): dated the day before the next T so it closes at that T.
        entries[0].Date.Should().Be(Today.AddDays(-1));
        entries[0].ActivatedAt.Should().Be(Clock.UtcNow);
        entries[1].Date.Should().Be(Today);
        entries[1].ActivatedAt.Should().BeNull();

        var status = await NewDailyService(db).GetCurrentStatusAsync(group.Id, beaId);
        status.Today.Status.Should().Be("voting");
        status.Today.ClosesAt.Should().Be(DailyClock.ToUtc(Today, DailyTime));
        status.Selection!.Date.Should().Be(Today);
    }
}
