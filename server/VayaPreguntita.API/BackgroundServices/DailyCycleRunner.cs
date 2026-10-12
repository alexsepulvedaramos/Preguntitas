using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Services;

namespace VayaPreguntita.API.BackgroundServices;

// One tick of the daily cycle (§4.3): preselect tomorrow's question once T has passed,
// activate today's at T, remind selectors who haven't chosen, and send the "streak in
// danger" pushes. DailyPreselectionService runs it every minute in its own DI scope;
// tests run it directly at a fixed instant.
public class DailyCycleRunner(
    AppDbContext context,
    IDailyService dailyService,
    INotificationService notificationService,
    IStreakService streakService,
    ICoinService coinService,
    TimeProvider clock,
    ILogger<DailyCycleRunner> logger
)
{
    // After this many hours without a manual selection, send a reminder to the selector.
    public static readonly TimeSpan SelectorReminderDelay = TimeSpan.FromHours(3);

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
                _ = notificationService.SendNewQuestionAsync(
                    entry.GroupId, entry.Group.Name, entry.Question.Text, entry.Question.TextTranslations);
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

        // ── STREAK IN DANGER (rama 19) ────────────────────────────────────────
        await streakService.SendDangerRemindersAsync();
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
}
