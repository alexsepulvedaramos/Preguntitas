namespace VayaPreguntita.API.BackgroundServices;

// Drives the daily cycle: runs one DailyCycleRunner tick per minute, each in its own scope.
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
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<DailyCycleRunner>().RunOnceAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en DailyPreselectionService");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
