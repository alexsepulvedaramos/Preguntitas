using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

// Seasonal frames (rama 57, §17.15 item 8): the "it's in the shop" push to every player, once
// per item, in the evening of its first day on sale. Run by the daily cycle tick.
public class SeasonalAnnouncementService(
    AppDbContext context,
    INotificationService notificationService,
    TimeProvider clock,
    ILogger<SeasonalAnnouncementService> logger
) : ISeasonalAnnouncementService
{
    // The push goes out from 20:00 (Madrid); if the server was down or deployed later, it
    // waits for the next evening rather than waking anyone up.
    public static readonly TimeOnly AnnounceFrom = new(20, 0);
    public static readonly TimeOnly AnnounceUntil = new(23, 0);

    public async Task AnnounceDueAsync()
    {
        var localTime = DailyClock.TimeOfDay(clock);
        if (localTime < AnnounceFrom || localTime >= AnnounceUntil)
            return;

        var now = DailyClock.UtcNow(clock);
        foreach (var item in ShopCatalog.All.Where(i => i.IsSeasonal && i.IsOnSale(now)))
        {
            var key = $"seasonal:{item.Key}";
            if (await context.Announcements.AnyAsync(a => a.Key == key))
                continue;

            // The row first: a second instance or a restart racing this one fails on the key
            // and sends nothing.
            context.Announcements.Add(new Announcement { Key = key, SentAt = now });
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                context.ChangeTracker.Clear();
                continue;
            }

            var daysLeft = (int)Math.Ceiling((item.OnSaleUntil!.Value - now).TotalDays);
            logger.LogInformation("Announcing seasonal item {Key} ({Days} days left)", item.Key, daysLeft);
            _ = notificationService.SendSeasonalItemAsync(item.Key, item.Price, daysLeft);
        }
    }
}
