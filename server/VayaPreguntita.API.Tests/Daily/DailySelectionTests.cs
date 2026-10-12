using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Daily;

// The selector chooses the NEXT day's question and it never activates early (§4.2), and
// only the selector sees it before T (§4.7).
[Collection(PostgresCollection.Name)]
public class DailySelectionTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Today = new(2026, 10, 5);
    private static readonly DateOnly Tomorrow = Today.AddDays(1);

    // Today's question is open; tomorrow's was preselected automatically from a pack and
    // carlos was drawn to choose.
    private async Task<(SeededGroup Group, int AutoPickId)> SeedPendingSelectionAsync()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        await AddActivatedDayAsync(group.Id, Today, group["bea"]);
        var autoPick = await AddQuestionAsync(
            group.Id,
            text: "Clon automático de un pack",
            configure: q => q.Source = QuestionSource.Pack
        );
        await AddEntryAsync(group.Id, Tomorrow, autoPick, group["carlos"], activated: false);
        Clock.SetLocal(Today, new TimeOnly(22, 0));
        return (group, autoPick);
    }

    private async Task<SelectResult> SelectAsync(int groupId, int userId, SelectQuestionDto dto)
    {
        await using var db = NewContext();
        return await NewDailyService(db).SelectQuestionAsync(groupId, userId, dto);
    }

    [Fact]
    public async Task OnlyTheDaysSelectorCanChoose()
    {
        var (group, autoPick) = await SeedPendingSelectionAsync();
        var poolQuestion = await AddQuestionAsync(group.Id, creatorId: group["bea"]);

        var result = await SelectAsync(group.Id, group["bea"], new() { ExistingQuestionId = poolQuestion });

        result.Should().Be(SelectResult.NotYourTurn);
        await using var db = NewContext();
        (await db.DailyEntries.SingleAsync(d => d.Date == Tomorrow)).QuestionId.Should().Be(autoPick);
    }

    [Fact]
    public async Task ChoosingAPoolQuestion_ReplacesTheAutomaticPickWithoutActivatingIt()
    {
        var (group, autoPick) = await SeedPendingSelectionAsync();
        var poolQuestion = await AddQuestionAsync(group.Id, creatorId: group["ana"]);

        var result = await SelectAsync(group.Id, group["carlos"], new() { ExistingQuestionId = poolQuestion });

        result.Should().Be(SelectResult.Success);
        await using var db = NewContext();
        var entry = await db.DailyEntries.Include(d => d.Question).SingleAsync(d => d.Date == Tomorrow);
        entry.QuestionId.Should().Be(poolQuestion);
        entry.SelectionKind.Should().Be(SelectionKind.Picked);
        entry.ActivatedAt.Should().BeNull();
        entry.Question.IsUsed.Should().BeTrue();
        entry.Question.DateActivated.Should().BeNull();
        (await db.Questions.AnyAsync(q => q.Id == autoPick))
            .Should()
            .BeFalse("an unused pack clone has no pool value and is discarded");
    }

    [Fact]
    public async Task ChangingYourMind_ReturnsThePreviousPickToThePool()
    {
        var (group, _) = await SeedPendingSelectionAsync();
        var first = await AddQuestionAsync(group.Id, text: "Primera opción", creatorId: group["ana"]);
        var second = await AddQuestionAsync(group.Id, text: "Segunda opción", creatorId: group["bea"]);

        await SelectAsync(group.Id, group["carlos"], new() { ExistingQuestionId = first });
        var result = await SelectAsync(group.Id, group["carlos"], new() { ExistingQuestionId = second });

        result.Should().Be(SelectResult.Success);
        await using var db = NewContext();
        (await db.DailyEntries.SingleAsync(d => d.Date == Tomorrow)).QuestionId.Should().Be(second);
        (await db.Questions.SingleAsync(q => q.Id == first)).IsUsed.Should().BeFalse();
    }

    [Fact]
    public async Task ATemplateAlreadyAskedInTheGroup_CannotBePickedWhileOthersRemain()
    {
        var (group, _) = await SeedPendingSelectionAsync();
        var pack = await AddPackAsync("Base");
        var asked = await AddTemplateAsync(pack, "Ya salió");
        var fresh = await AddTemplateAsync(pack, "Todavía no");
        await AddQuestionAsync(
            group.Id,
            isUsed: true,
            configure: q =>
            {
                q.TemplateId = asked;
                q.Source = QuestionSource.Pack;
            }
        );

        var repeat = await SelectAsync(group.Id, group["carlos"], new() { TemplateId = asked });
        var other = await SelectAsync(group.Id, group["carlos"], new() { TemplateId = fresh });

        repeat.Should().Be(SelectResult.TemplateAlreadyUsed);
        other.Should().Be(SelectResult.Success);
        await using var db = NewContext();
        var entry = await db.DailyEntries.Include(d => d.Question).SingleAsync(d => d.Date == Tomorrow);
        entry.Question.TemplateId.Should().Be(fresh);
        entry.Question.Source.Should().Be(QuestionSource.Pack);
    }

    [Fact]
    public async Task BeforeTheNextSelectorIsDrawn_NobodyCanChoose()
    {
        // Yesterday's question is still open and the tick hasn't queued today's yet.
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        await AddActivatedDayAsync(group.Id, Today.AddDays(-1), group["ana"]);
        var chosen = await AddQuestionAsync(group.Id, text: "La elegida", creatorId: group["ana"]);
        Clock.SetLocal(Today, new TimeOnly(10, 0));

        var results = new List<SelectResult>();
        foreach (var userId in group.UserIds.Values)
            results.Add(await SelectAsync(group.Id, userId, new() { ExistingQuestionId = chosen }));

        results.Should().OnlyContain(r => r == SelectResult.NotYourTurn);
        await using var db = NewContext();
        (await db.DailyEntries.CountAsync()).Should().Be(1);
        var status = await NewDailyService(db).GetCurrentStatusAsync(group.Id, group["ana"]);
        status.Today.Status.Should().Be("voting");
        status.Selection.Should().BeNull("the next selector hasn't been drawn yet");
    }

    [Fact]
    public async Task CreatingAQuestionInline_IsRecordedAsCreated()
    {
        var (group, _) = await SeedPendingSelectionAsync();

        var result = await SelectAsync(
            group.Id,
            group["carlos"],
            new()
            {
                NewQuestion = new()
                {
                    Text = "¿Quién se perdería en su propio barrio?",
                    Type = QuestionType.Superlative,
                },
            }
        );

        result.Should().Be(SelectResult.Success);
        await using var db = NewContext();
        var entry = await db.DailyEntries.Include(d => d.Question).SingleAsync(d => d.Date == Tomorrow);
        entry.SelectionKind.Should().Be(SelectionKind.Created);
        entry.Question.CreatorId.Should().Be(group["carlos"]);
    }

    [Fact]
    public async Task ConfirmingTheAutomaticPick_KeepsTheQuestionAndCountsAsAChoice()
    {
        var (group, autoPick) = await SeedPendingSelectionAsync();

        await using var db = NewContext();
        var service = NewDailyService(db);
        var notSelector = await service.ConfirmSelectionAsync(group.Id, group["ana"]);
        var selector = await service.ConfirmSelectionAsync(group.Id, group["carlos"]);
        var view = await service.GetCurrentStatusAsync(group.Id, group["carlos"]);

        notSelector.Should().Be(SelectResult.NotYourTurn);
        selector.Should().Be(SelectResult.Success);
        var entry = await db.DailyEntries.AsNoTracking().SingleAsync(d => d.Date == Tomorrow);
        entry.QuestionId.Should().Be(autoPick);
        entry.SelectionKind.Should().Be(SelectionKind.Confirmed);
        entry.ActivatedAt.Should().BeNull();
        view.Selection!.IsAutoSelected.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmingAfterPicking_DoesNotDowngradeTheChoice()
    {
        var (group, _) = await SeedPendingSelectionAsync();
        var poolQuestion = await AddQuestionAsync(group.Id, creatorId: group["ana"]);
        await SelectAsync(group.Id, group["carlos"], new() { ExistingQuestionId = poolQuestion });

        await using var db = NewContext();
        var result = await NewDailyService(db).ConfirmSelectionAsync(group.Id, group["carlos"]);

        result.Should().Be(SelectResult.Success);
        (await db.DailyEntries.SingleAsync(d => d.Date == Tomorrow))
            .SelectionKind.Should()
            .Be(SelectionKind.Picked);
    }

    [Fact]
    public async Task TomorrowsQuestionStaysASurpriseForEveryoneButTheSelector()
    {
        var (group, autoPick) = await SeedPendingSelectionAsync();
        await using var db = NewContext();
        var service = NewDailyService(db);

        var selectorView = await service.GetCurrentStatusAsync(group.Id, group["carlos"]);
        var memberView = await service.GetCurrentStatusAsync(group.Id, group["ana"]);

        selectorView.Selection!.IsCurrentUserSelector.Should().BeTrue();
        selectorView.Selection.PendingQuestion!.Id.Should().Be(autoPick);
        memberView.Selection!.IsCurrentUserSelector.Should().BeFalse();
        memberView.Selection.SelectorUsername.Should().Be("carlos");
        memberView.Selection.PendingQuestion.Should().BeNull();

        // T is 21:00 in UTC+2: today's question closes and tomorrow's opens at 19:00 UTC.
        var nextT = new DateTime(2026, 10, 6, 19, 0, 0, DateTimeKind.Utc);
        memberView.Today.Status.Should().Be("voting");
        memberView.Today.ClosesAt.Should().Be(nextT);
        memberView.Selection.ActivatesAt.Should().Be(nextT);
    }
}
