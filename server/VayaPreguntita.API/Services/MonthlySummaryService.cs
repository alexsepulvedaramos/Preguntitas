using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.MonthlySummaries;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

public class MonthlySummaryService(
    AppDbContext context,
    INotificationService notificationService,
    TimeProvider clock,
    ILogger<MonthlySummaryService> logger
) : IMonthlySummaryService
{
    // Groups built per tick (one tick a minute): the 00:00 burst on the 1st is spread out
    // so it never competes with requests (§17.12).
    public const int BatchSize = 5;

    // The group page reaches the last 12 months.
    public const int ListedMonths = 12;

    private const string MonthFormat = "yyyy-MM";

    // The month that just ended, as its first local day.
    private DateOnly PreviousMonth()
    {
        var today = DailyClock.Today(clock);
        return new DateOnly(today.Year, today.Month, 1).AddMonths(-1);
    }

    public async Task BuildPendingAsync(int batchSize = BatchSize)
    {
        var month = PreviousMonth();
        var monthEnd = month.AddMonths(1);

        // Any day of the month, not only the 1st: a sleeping server catches up on wake.
        var groups = await context.Groups
            .Where(g =>
                context.DailyEntries.Any(d =>
                    d.GroupId == g.Id && d.ActivatedAt != null && d.Date >= month && d.Date < monthEnd)
                && !context.MonthlySummaries.Any(s => s.GroupId == g.Id && s.Month == month))
            .OrderBy(g => g.Id)
            .Select(g => new { g.Id, g.Name })
            .Take(batchSize)
            .ToListAsync();

        var today = DailyClock.Today(clock);
        foreach (var group in groups)
        {
            try
            {
                var summary = await BuildAsync(group.Id, month);
                logger.LogInformation("Resumen mensual {Month} creado para grupo {GroupId}", month, group.Id);

                // Today's question already opened: its push went out without the summary,
                // so send it now. Otherwise it goes out with today's activation.
                var openedToday = await context.DailyEntries.AnyAsync(d =>
                    d.GroupId == group.Id && d.Date == today && d.ActivatedAt != null);
                if (openedToday)
                    await NotifyAsync(summary, group.Name);
            }
            catch (Exception ex)
            {
                context.ChangeTracker.Clear();
                logger.LogError(ex, "Monthly summary failed for group {GroupId}", group.Id);
            }
        }
    }

    public async Task NotifyIfReadyAsync(int groupId, string groupName)
    {
        var month = PreviousMonth();
        var summary = await context.MonthlySummaries
            .FirstOrDefaultAsync(s => s.GroupId == groupId && s.Month == month && s.NotifiedAt == null);
        if (summary != null)
            await NotifyAsync(summary, groupName);
    }

    private async Task NotifyAsync(MonthlySummary summary, string groupName)
    {
        summary.NotifiedAt = DailyClock.UtcNow(clock);
        await context.SaveChangesAsync();
        _ = notificationService.SendMonthlySummaryAsync(summary.GroupId, groupName, summary.Month);
    }

    private async Task<MonthlySummary> BuildAsync(int groupId, DateOnly month)
    {
        var monthEnd = month.AddMonths(1);

        var entries = (await context.DailyEntries
            .Where(d => d.GroupId == groupId && d.ActivatedAt != null && d.Date >= month && d.Date < monthEnd)
            .Select(d => new
            {
                d.Id,
                d.Date,
                ActivatedAt = d.ActivatedAt!.Value,
                d.QuestionId,
                d.Question.Type,
                d.Question.Text,
                d.Question.Source,
                d.Question.CreatorId,
                d.SelectorUserId,
                d.SelectionKind,
            })
            .ToListAsync())
            .Select(d => new MonthlySummaryBuilder.EntryData(
                d.Id, d.Date, d.ActivatedAt, d.QuestionId, d.Type, d.Text,
                // Member-written only (§17.6): pack clones have no author.
                d.Source == QuestionSource.UserCreated ? d.CreatorId : null,
                d.SelectorUserId, d.SelectionKind))
            .ToList();

        var questionIds = entries.Select(e => e.QuestionId).ToList();
        var votes = (await context.Votes
            .Where(v => questionIds.Contains(v.QuestionId) && !v.IsLate)
            .Select(v => new { v.QuestionId, v.UserId, v.SelectedTargetUserId })
            .ToListAsync())
            .Select(v => new MonthlySummaryBuilder.VoteData(v.QuestionId, v.UserId, v.SelectedTargetUserId))
            .ToList();

        // A correct guess only counts next to an on-time vote: late votes never reach the
        // monthly awards (§17.7).
        var questionByEntry = entries.ToDictionary(e => e.EntryId, e => e.QuestionId);
        var onTime = votes.Select(v => (v.QuestionId, v.UserId)).ToHashSet();
        var entryIds = questionByEntry.Keys.ToList();
        var correctGuesses = (await context.AuthorGuesses
            .Where(g => entryIds.Contains(g.DailyEntryId) && g.IsCorrect && g.GuessedAt != null)
            .Select(g => new { g.DailyEntryId, g.UserId })
            .ToListAsync())
            .Where(g => onTime.Contains((questionByEntry[g.DailyEntryId], g.UserId)))
            .Select(g => (g.DailyEntryId, g.UserId))
            .ToList();

        var members = await context.GroupMembers
            .Where(m => m.GroupId == groupId)
            .Select(m => new MonthlySummaryBuilder.MemberData(m.UserId, m.JoinedAt))
            .ToListAsync();

        var payload = MonthlySummaryBuilder.Build(entries, members, votes, correctGuesses);
        var summary = new MonthlySummary
        {
            GroupId = groupId,
            Month = month,
            Payload = JsonSerializer.Serialize(payload),
            CreatedAt = DailyClock.UtcNow(clock),
        };
        context.MonthlySummaries.Add(summary);
        await context.SaveChangesAsync();
        return summary;
    }

    public async Task<List<MonthlySummaryListItemDto>> ListAsync(int groupId)
    {
        var rows = await context.MonthlySummaries
            .Where(s => s.GroupId == groupId)
            .OrderByDescending(s => s.Month)
            .Take(ListedMonths)
            .Select(s => new { s.Month, s.Payload })
            .ToListAsync();

        return rows
            .Select(r => new MonthlySummaryListItemDto(
                r.Month.ToString(MonthFormat, CultureInfo.InvariantCulture),
                Deserialize(r.Payload).Questions))
            .ToList();
    }

    public async Task<MonthlySummaryDto?> GetAsync(int groupId, int userId, string month)
    {
        if (!DateOnly.TryParseExact(month, MonthFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
            return null;

        var payloadJson = await context.MonthlySummaries
            .Where(s => s.GroupId == groupId && s.Month == start)
            .Select(s => s.Payload)
            .FirstOrDefaultAsync();
        if (payloadJson == null)
            return null;

        var payload = Deserialize(payloadJson);
        var myMatch = payload.Matches.FirstOrDefault(m => m.UserId == userId);

        var userIds = payload.Members.Select(m => m.UserId)
            .Concat(payload.Awards.SelectMany(a => a.Winners.Select(w => w.UserId)))
            .Concat(myMatch?.MatchUserIds ?? [])
            .Distinct()
            .ToList();
        var people = await context.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new SummaryPersonDto(u.Id, u.Username, u.AvatarUrl))
            .ToDictionaryAsync(u => u.UserId);

        // A deleted account drops out of the summary instead of breaking it.
        SummaryPersonDto? Person(int id) => people.GetValueOrDefault(id);

        var awards = payload.Awards
            .Select(a => new SummaryAwardDto(
                a.Key,
                a.Value,
                a.Winners
                    .Where(w => Person(w.UserId) != null)
                    .Select(w => new SummaryAwardWinnerDto(Person(w.UserId)!, w.QuestionText))
                    .ToList()))
            .Where(a => a.Winners.Count > 0)
            .ToList();

        var members = payload.Members
            .Where(m => Person(m.UserId) != null)
            .Select(m => new SummaryMemberDto(
                Person(m.UserId)!, m.Answered, m.Eligible,
                MonthlySummaryBuilder.Percent(m.Answered, m.Eligible), m.BestRun))
            .OrderBy(m => m.Person.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var matchPeople = myMatch?.MatchUserIds.Select(Person).OfType<SummaryPersonDto>().ToList() ?? [];

        return new MonthlySummaryDto(
            month,
            payload.Questions,
            MonthlySummaryBuilder.Percent(payload.Answered, payload.Eligible),
            awards,
            members,
            matchPeople.Count > 0 ? new SummaryMatchDto(matchPeople, myMatch!.Count) : null
        );
    }

    private static MonthlySummaryPayload Deserialize(string json) =>
        JsonSerializer.Deserialize<MonthlySummaryPayload>(json)
        ?? throw new InvalidOperationException("Empty monthly summary payload");
}
