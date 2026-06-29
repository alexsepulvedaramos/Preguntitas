using System.Text.Json;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Options;
using LibPushSubscription = Lib.Net.Http.WebPush.PushSubscription;

namespace VayaPreguntita.API.Services;

public class NotificationService(
    AppDbContext context,
    PushServiceClient pushClient,
    ILogger<NotificationService> logger
) : INotificationService
{
    private const string IconPath = "/icons/android-chrome-192x192.png";

    public async Task SendNewQuestionAsync(int groupId, string groupName, string questionText)
    {
        var recipients = await GetEligibleMembersAsync(groupId, excludeUserId: null, pref => pref.NewQuestion);
        var body = questionText.Length > 80 ? questionText[..77] + "…" : questionText;
        var payload = BuildPayload($"❓ {groupName}", body, $"/groups/{groupId}");
        await SendToManyAsync(recipients, payload);
    }

    public async Task SendSelectorTurnAsync(int groupId, int selectorUserId, string groupName, TimeOnly dailyTime)
    {
        var recipients = await GetEligibleMembersAsync(groupId, excludeUserId: null, pref => pref.SelectorTurn);
        recipients = recipients.Where(r => r.UserId == selectorUserId).ToList();
        var timeStr = dailyTime.ToString("HH:mm");
        var payload = BuildPayload(
            $"🎯 ¡Te toca elegir en {groupName}!",
            $"Eres el selector de hoy. Elige la pregunta antes de las {timeStr}.",
            $"/groups/{groupId}"
        );
        await SendToManyAsync(recipients, payload);
    }

    public async Task SendUserVotedAsync(int groupId, int voterUserId, string groupName, string voterUsername)
    {
        var recipients = await GetEligibleMembersAsync(groupId, excludeUserId: voterUserId, pref => pref.UserVoted);
        var payload = BuildPayload(
            groupName,
            $"{voterUsername} ha votado. ¿Cuál será su respuesta? 🗳️",
            $"/groups/{groupId}"
        );
        await SendToManyAsync(recipients, payload);
    }

    public async Task SendNewMessageAsync(int groupId, int senderUserId, string groupName, string senderUsername, string messagePreview)
    {
        var recipients = await GetEligibleMembersAsync(groupId, excludeUserId: senderUserId, pref => pref.NewMessage);
        var preview = messagePreview.Length > 80 ? messagePreview[..77] + "…" : messagePreview;
        var payload = BuildPayload($"💬 {groupName}", $"{senderUsername}: {preview}", $"/groups/{groupId}");
        await SendToManyAsync(recipients, payload);
    }

    // Returns subscriptions for eligible members: in the group, not muted, pref enabled (or no pref row = default true).
    private async Task<List<DevicePushSubscription>> GetEligibleMembersAsync(
        int groupId,
        int? excludeUserId,
        Func<NotificationPreferences, bool> prefSelector)
    {
        var memberIds = await context.GroupMembers
            .Where(gm => gm.GroupId == groupId
                && !gm.NotificationsMuted
                && (excludeUserId == null || gm.UserId != excludeUserId))
            .Select(gm => gm.UserId)
            .ToListAsync();

        if (memberIds.Count == 0) return [];

        // Members with an explicit pref row that disables this type are excluded.
        var disabledUserIds = await context.NotificationPreferences
            .Where(p => memberIds.Contains(p.UserId) && !prefSelector(p))
            .Select(p => p.UserId)
            .ToListAsync();

        var eligibleUserIds = memberIds.Except(disabledUserIds).ToList();

        return await context.DevicePushSubscriptions
            .Where(s => eligibleUserIds.Contains(s.UserId))
            .ToListAsync();
    }

    private async Task SendToManyAsync(IEnumerable<DevicePushSubscription> subscriptions, string jsonPayload)
    {
        var message = new PushMessage(jsonPayload) { TimeToLive = 3600 };
        var toRemove = new List<int>();

        foreach (var sub in subscriptions)
        {
            try
            {
                var libSub = new LibPushSubscription
                {
                    Endpoint = sub.Endpoint,
                    Keys = new Dictionary<string, string>
                    {
                        ["auth"] = sub.Auth,
                        ["p256dh"] = sub.P256dh
                    }
                };
                await pushClient.RequestPushMessageDeliveryAsync(libSub, message);
            }
            catch (PushServiceClientException ex) when (ex.StatusCode is
                System.Net.HttpStatusCode.Gone or
                System.Net.HttpStatusCode.NotFound)
            {
                // Subscription expired or revoked — remove it.
                toRemove.Add(sub.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send push to subscription {Id}", sub.Id);
            }
        }

        if (toRemove.Count > 0)
        {
            var stale = context.DevicePushSubscriptions.Where(s => toRemove.Contains(s.Id));
            context.DevicePushSubscriptions.RemoveRange(stale);
            await context.SaveChangesAsync();
        }
    }

    private static string BuildPayload(string title, string body, string url) =>
        JsonSerializer.Serialize(new
        {
            notification = new
            {
                title,
                body,
                icon = IconPath,
                badge = IconPath,
                data = new { url }
            }
        });
}
