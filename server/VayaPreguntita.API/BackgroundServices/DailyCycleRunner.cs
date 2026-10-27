using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Services;

namespace VayaPreguntita.API.BackgroundServices;

// One tick of the daily cycle (§4.3): preselect tomorrow's question once T has passed,
// activate today's at T, remind selectors who haven't chosen, open the race to select at
// T − 10 min, send the "streak in danger" pushes, build last month's summaries and announce
// seasonal frames (rama 57). DailyPreselectionService runs it every minute in its own DI scope;
// tests run it directly at a fixed instant.
public class DailyCycleRunner(
    AppDbContext context,
    IDailyService dailyService,
    INotificationService notificationService,
    IStreakService streakService,
    ICoinService coinService,
    IMonthlySummaryService monthlySummaryService,
    IFlashService flashService,
    ISeasonalAnnouncementService seasonalAnnouncementService,
    TimeProvider clock,
    ILogger<DailyCycleRunner> logger
)
{
    // After this many hours without a manual selection, send a reminder to the selector.
    public static readonly TimeSpan SelectorReminderDelay = TimeSpan.FromHours(3);

    // How long before T the race to select opens if the selector hasn't chosen (§17.5).
    public static readonly TimeSpan RaceLead = TimeSpan.FromMinutes(10);

    public async Task RunOnceAsync()
    {
        var today = DailyClock.Today(clock);
        var tomorrow = today.AddDays(1);
        var localTimeOfDay = DailyClock.TimeOfDay(clock);

        // ── PRESELECT TOMORROW ────────────────────────────────────────────────
        var groupsToPreselect = await context
            .Groups.Where(g =>
                localTimeOfDay >= g.DailyQuestionTime
                && g.Members.Count >= 2
                && !context.DailyEntries.Any(d => d.GroupId == g.Id && d.Date == tomorrow)
            )
            .Select(g => new { g.Id, g.Name, g.DailyQuestionTime })
            .ToListAsync();

        foreach (var group in groupsToPreselect)
        {
            var selectorUserId = await dailyService.PreselectForGroupAsync(group.Id, tomorrow);
            logger.LogInformation(
                "Preseleccionada pregunta para grupo {GroupId} fecha {Date}",
                group.Id,
                tomorrow
            );

            if (selectorUserId.HasValue)
                _ = notificationService.SendSelectorTurnAsync(
                    group.Id, selectorUserId.Value, group.Name, group.DailyQuestionTime);
        }

        // ── ACTIVATE TODAY ────────────────────────────────────────────────────
        var entriesToActivate = await context
            .DailyEntries.Include(d => d.Group)
            .Include(d => d.Question)
            .Include(d => d.Selector)
            .Where(d =>
                d.Date == today
                && d.ActivatedAt == null
                && localTimeOfDay >= d.Group.DailyQuestionTime
            )
            .ToListAsync();

        var nowUtc = DailyClock.UtcNow(clock);
        foreach (var entry in entriesToActivate)
        {
            entry.ActivatedAt = nowUtc;
            entry.Question.DateActivated = nowUtc;
            logger.LogInformation(
                "Activada pregunta {QuestionId} para grupo {GroupId}",
                entry.QuestionId,
                entry.GroupId
            );
        }

        if (entriesToActivate.Count > 0)
        {
            await context.SaveChangesAsync();

            foreach (var entry in entriesToActivate)
                await PaySelectorRewardAsync(entry);

            foreach (var entry in entriesToActivate)
                await PayAuthorRewardAsync(entry);

            foreach (var entry in entriesToActivate)
                await CloseCycleAsync(entry);

            foreach (var entry in entriesToActivate)
                _ = notificationService.SendNewQuestionAsync(
                    entry.GroupId, entry.Group.Name, entry.Question.Text, entry.Question.TextTranslations,
                    DuelPushFor(entry));

            // Last month's summary goes out with the activation, adding no new burst (§17.12).
            foreach (var entry in entriesToActivate)
                await NotifyMonthlySummaryAsync(entry);
        }

        // ── SELECTOR REMINDER (if no manual selection after SelectorReminderDelay) ──
        var reminderCutoff = nowUtc - SelectorReminderDelay;
        var pendingWithoutSelection = await context
            .DailyEntries.Include(d => d.Group)
            .Where(d =>
                d.ActivatedAt == null
                && d.SelectionKind == SelectionKind.Auto
                && d.PreselectedAt <= reminderCutoff
                && d.SelectorReminderSentAt == null
            )
            .ToListAsync();

        foreach (var entry in pendingWithoutSelection)
        {
            entry.SelectorReminderSentAt = nowUtc;
            _ = notificationService.SendSelectorTurnAsync(
                entry.GroupId, entry.SelectorUserId, entry.Group.Name, entry.Group.DailyQuestionTime);
            logger.LogInformation(
                "Recordatorio de selector enviado para grupo {GroupId}",
                entry.GroupId
            );
        }

        if (pendingWithoutSelection.Count > 0)
            await context.SaveChangesAsync();

        // ── RACE TO SELECT (§17.5): T − 10 min and still auto-selected ─────────────
        await OpenRacesAsync(nowUtc);

        // ── STREAK IN DANGER (rama 19) ────────────────────────────────────────
        await streakService.SendDangerRemindersAsync();

        // ── MONTHLY SUMMARY (§17.12): a few groups per tick ────────────────────
        await monthlySummaryService.BuildPendingAsync();

        // ── TIME CAPSULES (rama 50): push the ones whose results just opened ──
        await flashService.OpenDueCapsulesAsync();

        // ── SEASONAL FRAMES (rama 57): the "in the shop" push, once, at 16:00 ────
        await seasonalAnnouncementService.AnnounceDueAsync();
    }

    // A 1-vs-1 Deathmatch the selector actively chose is a duel (§17.15 item 6); a pure
    // auto-selection isn't, so no duel happens without someone wanting it.
    private static DuelPush? DuelPushFor(DailyEntry entry) =>
        DuelRules.IsDailyDuel(entry, entry.Question) && DuelRules.Duelists(entry.Question) is (int first, int second)
            ? new DuelPush(entry.SelectorUserId, entry.Selector.Username, first, second)
            : null;

    private async Task NotifyMonthlySummaryAsync(DailyEntry entry)
    {
        try
        {
            await monthlySummaryService.NotifyIfReadyAsync(entry.GroupId, entry.Group.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Monthly summary push failed for group {GroupId}", entry.GroupId);
        }
    }

    // A pending entry is due for the race when its activation (Date at T) is at most
    // RaceLead away: the activation check, run on the clock shifted RaceLead ahead.
    private async Task OpenRacesAsync(DateTime nowUtc)
    {
        var raceLocal = DailyClock.Now(clock) + RaceLead;
        var raceDate = DateOnly.FromDateTime(raceLocal);
        var raceTime = TimeOnly.FromDateTime(raceLocal);

        var entries = await context
            .DailyEntries.Include(d => d.Group)
            .Where(d =>
                d.ActivatedAt == null
                && d.SelectionKind == SelectionKind.Auto
                && d.RaceOpenedAt == null
                && (d.Date < raceDate || (d.Date == raceDate && raceTime >= d.Group.DailyQuestionTime))
            )
            .ToListAsync();
        if (entries.Count == 0)
            return;

        foreach (var entry in entries)
            entry.RaceOpenedAt = nowUtc;
        await context.SaveChangesAsync();

        foreach (var entry in entries)
        {
            logger.LogInformation("Carrera por elegir abierta en grupo {GroupId}", entry.GroupId);
            _ = notificationService.SendRaceToSelectAsync(
                entry.GroupId, entry.SelectorUserId, entry.Group.Name, entry.Group.DailyQuestionTime);
        }
    }

    // Activating an entry closes the previous cycle: streak freezes are spent and lost
    // streaks recorded for repair (§17.9).
    private async Task CloseCycleAsync(DailyEntry entry)
    {
        try
        {
            await streakService.CloseCycleAsync(entry.GroupId, entry.Date);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Close of cycle failed for group {GroupId}", entry.GroupId);
        }
    }

    // Selector coins (§17.2) are paid at activation: until T the selector can still change
    // their choice. Auto-selected questions earn nothing.
    private async Task PaySelectorRewardAsync(DailyEntry entry)
    {
        if (entry.SelectionKind == SelectionKind.Auto)
            return;
        try
        {
            await coinService.GrantAsync(
                entry.SelectorUserId,
                CoinReason.SelectorChoice,
                CoinRewards.SelectorChoice,
                entry.GroupId,
                entry.Id
            );
            if (entry.SelectionKind == SelectionKind.Created)
                await coinService.GrantAsync(
                    entry.SelectorUserId,
                    CoinReason.SelectorCreated,
                    CoinRewards.SelectorCreatedBonus,
                    entry.GroupId,
                    entry.Id
                );
        }
        catch (Exception ex)
        {
            // Never block the rest of the tick over a reward.
            logger.LogError(ex, "Selector reward failed for entry {EntryId}", entry.Id);
        }
    }

    // The author of a member-written question is paid when it's played (§17.2), while still a
    // member. Whoever chose their own question gets the selector rewards instead.
    private async Task PayAuthorRewardAsync(DailyEntry entry)
    {
        if (AuthorGuessRules.AuthorOf(entry.Question) is not int authorId)
            return;
        if (authorId == entry.SelectorUserId && entry.SelectionKind != SelectionKind.Auto)
            return;
        try
        {
            var isMember = await context.GroupMembers.AnyAsync(m =>
                m.GroupId == entry.GroupId && m.UserId == authorId);
            if (!isMember)
                return;
            await coinService.GrantAsync(
                authorId,
                CoinReason.QuestionPlayed,
                CoinRewards.QuestionPlayed,
                entry.GroupId,
                entry.Id
            );
        }
        catch (Exception ex)
        {
            // Never block the rest of the tick over a reward.
            logger.LogError(ex, "Author reward failed for entry {EntryId}", entry.Id);
        }
    }
}
