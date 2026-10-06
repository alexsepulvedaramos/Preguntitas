using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Services;

namespace VayaPreguntita.API.BackgroundServices;

public class DailyPreselectionService(
    IServiceScopeFactory scopeFactory,
    ILogger<DailyPreselectionService> logger
) : BackgroundService
{
    // After this many hours without a manual selection, send a reminder to the selector.
    private static readonly TimeSpan SelectorReminderDelay = TimeSpan.FromHours(3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndPreselect();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en DailyPreselectionService");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CheckAndPreselect()
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dailyService = scope.ServiceProvider.GetRequiredService<IDailyService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var today = DailyClock.Today();
        var tomorrow = today.AddDays(1);
        var localTimeOfDay = DailyClock.TimeOfDay();

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

        foreach (var entry in entriesToActivate)
        {
            entry.ActivatedAt = DateTime.UtcNow;
            entry.Question.DateActivated = DateTime.UtcNow;
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
                _ = notificationService.SendNewQuestionAsync(
                    entry.GroupId, entry.Group.Name, entry.Question.Text);
        }

        // ── SELECTOR REMINDER (if no manual selection after SelectorReminderDelay) ──
        var reminderCutoff = DateTime.UtcNow - SelectorReminderDelay;
        var pendingWithoutSelection = await context
            .DailyEntries.Include(d => d.Group)
            .Where(d =>
                d.ActivatedAt == null
                && d.IsAutoSelected
                && d.PreselectedAt <= reminderCutoff
                && d.SelectorReminderSentAt == null
            )
            .ToListAsync();

        foreach (var entry in pendingWithoutSelection)
        {
            entry.SelectorReminderSentAt = DateTime.UtcNow;
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
        var streakService = scope.ServiceProvider.GetRequiredService<IStreakService>();
        await streakService.SendDangerRemindersAsync();
    }
}
