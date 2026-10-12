using System.Globalization;
using System.Text.Json;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Localization;
using VayaPreguntita.API.Options;
using LibPushSubscription = Lib.Net.Http.WebPush.PushSubscription;

namespace VayaPreguntita.API.Services;

// Notifications are dispatched fire-and-forget by the calling services, so this
// service must never touch the caller's request-scoped DbContext (that would run
// a second concurrent operation on it and throw). Each send creates its own DI
// scope and resolves a fresh AppDbContext.
public class NotificationService(
    IServiceScopeFactory scopeFactory,
    PushServiceClient pushClient,
    TimeProvider clock,
    ILogger<NotificationService> logger
) : INotificationService
{
    private const string IconPath = "/icons/android-chrome-192x192.png";

    // How long the push service keeps an undelivered notification (a phone offline or
    // dozing). Chat and votes stay relevant for a while; a new question for its 24 h.
    private static readonly TimeSpan VotedTtl = TimeSpan.FromHours(6);
    private static readonly TimeSpan MessageTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan QuestionTtl = TimeSpan.FromHours(24);

    // Each dispatch runs fire-and-forget, so it must log its own failures — an exception
    // here (e.g. the recipients query) would otherwise vanish with the discarded task.
    // Text is written in each recipient's PreferredLanguage (rama 23): one payload per language.
    public Task SendNewQuestionAsync(int groupId, string groupName, string questionText) =>
        RunLoggedAsync("new_question", groupId, async context =>
        {
            var recipients = await GetEligibleMembersAsync(context, groupId, excludeUserId: null, pref => pref.NewQuestion);
            var body = questionText.Length > 80 ? questionText[..77] + "…" : questionText;
            await SendLocalizedAsync(context, recipients, QuestionTtl, lang =>
                BuildPayload($"❓ {groupName}", body, groupId, groupName, "new_question", lang));
        });

    public Task SendSelectorTurnAsync(int groupId, int selectorUserId, string groupName, TimeOnly dailyTime) =>
        RunLoggedAsync("selector_turn", groupId, async context =>
        {
            var recipients = await GetEligibleMembersAsync(context, groupId, excludeUserId: null, pref => pref.SelectorTurn);
            recipients = recipients.Where(r => r.Subscription.UserId == selectorUserId).ToList();
            var time = FormatTime(dailyTime);
            await SendLocalizedAsync(context, recipients, UntilNextDailyTime(dailyTime), lang =>
                BuildPayload(
                    ServerText.Push.SelectorTurnTitle(lang, groupName),
                    ServerText.Push.SelectorTurnBody(lang, time),
                    groupId, groupName, "selector_turn", lang
                ));
        });

    public Task SendUserVotedAsync(int groupId, int voterUserId, string groupName, string voterUsername) =>
        RunLoggedAsync("user_voted", groupId, async context =>
        {
            var recipients = await GetEligibleMembersAsync(context, groupId, excludeUserId: voterUserId, pref => pref.UserVoted);
            await SendLocalizedAsync(context, recipients, VotedTtl, lang =>
                BuildPayload(
                    groupName,
                    ServerText.Push.UserVotedBody(lang, voterUsername),
                    groupId, groupName, "user_voted", lang, actor: voterUsername
                ));
        });

    public Task SendNewMessageAsync(int groupId, int senderUserId, string groupName, string senderUsername, string messagePreview) =>
        RunLoggedAsync("new_message", groupId, async context =>
        {
            var recipients = await GetEligibleMembersAsync(context, groupId, excludeUserId: senderUserId, pref => pref.NewMessage);
            var preview = messagePreview.Length > 80 ? messagePreview[..77] + "…" : messagePreview;
            await SendLocalizedAsync(context, recipients, MessageTtl, lang =>
                BuildPayload($"💬 {groupName}", $"{senderUsername}: {preview}", groupId, groupName, "new_message", lang, actor: senderUsername));
        });

    public Task SendStreakDangerAsync(int groupId, int userId, string groupName, int streak, TimeOnly dailyTime) =>
        RunLoggedAsync("streak_danger", groupId, async context =>
        {
            var recipients = await context.DevicePushSubscriptions
                .Where(s => s.UserId == userId)
                .Select(s => new Recipient(s, s.User.PreferredLanguage))
                .ToListAsync();
            var time = FormatTime(dailyTime);
            await SendLocalizedAsync(context, recipients, UntilNextDailyTime(dailyTime), lang =>
                BuildPayload(
                    ServerText.Push.StreakDangerTitle(lang),
                    ServerText.Push.StreakDangerBody(lang, streak, groupName, time),
                    groupId, groupName, "streak_danger", lang
                ));
        });

    private sealed record Recipient(DevicePushSubscription Subscription, string Language);

    private static string FormatTime(TimeOnly time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);

    private async Task SendLocalizedAsync(
        AppDbContext context,
        IEnumerable<Recipient> recipients,
        TimeSpan ttl,
        Func<string, string> payloadFor)
    {
        foreach (var byLanguage in recipients.GroupBy(r => AppLanguages.Normalize(r.Language)))
            await SendToManyAsync(context, byLanguage.Select(r => r.Subscription), payloadFor(byLanguage.Key), ttl);
    }

    private async Task RunLoggedAsync(string kind, int groupId, Func<AppDbContext, Task> send)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await send(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to dispatch {Kind} notification for group {GroupId}", kind, groupId);
        }
    }

    // Selector turn and streak danger only matter until the cycle closes at the next T.
    private TimeSpan UntilNextDailyTime(TimeOnly dailyTime)
    {
        var nowUtc = DailyClock.UtcNow(clock);
        var nextT = DailyClock.ToUtc(DailyClock.Today(clock), dailyTime);
        if (nextT <= nowUtc) nextT = nextT.AddDays(1);
        var remaining = nextT - nowUtc;
        return remaining < TimeSpan.FromMinutes(5) ? TimeSpan.FromMinutes(5) : remaining;
    }

    // Returns subscriptions for eligible members: in the group, not muted, pref enabled (or no pref row = default true).
    private async Task<List<Recipient>> GetEligibleMembersAsync(
        AppDbContext context,
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
        // prefSelector runs client-side, so pull the candidate rows first.
        var prefRows = await context.NotificationPreferences
            .Where(p => memberIds.Contains(p.UserId))
            .ToListAsync();
        var disabledUserIds = prefRows.Where(p => !prefSelector(p)).Select(p => p.UserId).ToList();

        var eligibleUserIds = memberIds.Except(disabledUserIds).ToList();

        return await context.DevicePushSubscriptions
            .Where(s => eligibleUserIds.Contains(s.UserId))
            .Select(s => new Recipient(s, s.User.PreferredLanguage))
            .ToListAsync();
    }

    private async Task SendToManyAsync(AppDbContext context, IEnumerable<DevicePushSubscription> subscriptions, string jsonPayload, TimeSpan ttl)
    {
        var message = new PushMessage(jsonPayload) { TimeToLive = (int)ttl.TotalSeconds };
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

    private static string BuildPayload(string title, string body, int groupId, string groupName, string kind, string lang, string? actor = null)
    {
        var url = $"/groups/{groupId}";
        return JsonSerializer.Serialize(new
        {
            notification = new
            {
                title,
                body,
                icon = IconPath,
                badge = IconPath,
                // `onActionClick.default` tells the Angular service worker what to do when
                // the notification body is clicked while the app is closed/backgrounded:
                // focus an existing window (or open a new one at `url`). `url` is kept at
                // the top level too for the in-app SwPush.notificationClicks handler.
                // `kind`/`groupId`/`groupName`/`actor` let the client's sw.js merge a
                // group's notifications into one; `lang` picks the language of the merged text.
                data = new
                {
                    url,
                    kind,
                    groupId,
                    groupName,
                    actor,
                    lang,
                    onActionClick = new
                    {
                        @default = new { operation = "focusLastFocusedOrOpen", url }
                    }
                }
            }
        });
    }
}
