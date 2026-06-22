using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.Services;

namespace VayaPreguntita.API.BackgroundServices;

public class DailyPreselectionService(
    IServiceScopeFactory scopeFactory,
    ILogger<DailyPreselectionService> logger
) : BackgroundService
{
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

            // Comprobar cada minuto
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CheckAndPreselect()
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dailyService = scope.ServiceProvider.GetRequiredService<IDailyService>();

        var nowUtc = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(nowUtc);
        var tomorrow = today.AddDays(1);

        // Buscar grupos cuya hora de cierre ya ha pasado hoy
        // y que aún no tienen preselección para mañana
        var groupsToPreselect = await context
            .Groups.Where(g =>
                TimeOnly.FromDateTime(nowUtc) >= g.DailyQuestionTime
                && !context.DailyEntries.Any(d => d.GroupId == g.Id && d.Date == tomorrow)
            )
            .Select(g => g.Id)
            .ToListAsync();

        foreach (var groupId in groupsToPreselect)
        {
            await dailyService.PreselectForGroupAsync(groupId, tomorrow);
            logger.LogInformation(
                "Preseleccionada pregunta para grupo {GroupId} fecha {Date}",
                groupId,
                tomorrow
            );
        }

        // También activar las preselecciones de hoy cuya hora ya ha llegado
        var entriesToActivate = await context
            .DailyEntries.Include(d => d.Group)
            .Include(d => d.Question)
            .Where(d =>
                d.Date == today
                && d.ActivatedAt == null
                && TimeOnly.FromDateTime(nowUtc) >= d.Group.DailyQuestionTime
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
            await context.SaveChangesAsync();
    }
}
