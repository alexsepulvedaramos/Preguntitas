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
    // Android's status-bar icon only uses the alpha channel: the question mark alone, on
    // transparent. (The full-bleed app icon would show as a plain white square.)
    private const string BadgePath = "/icons/notification-badge.png";

    // How long the push service keeps an undelivered notification (a phone offline or
    // dozing). Chat and votes stay relevant for a while; a new question for its 24 h.
    private static readonly TimeSpan VotedTtl = TimeSpan.FromHours(6);
    private static readonly TimeSpan MessageTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan QuestionTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan SummaryTtl = TimeSpan.FromDays(3);

    // Each dispatch runs fire-and-forget, so it must log its own failures — an exception
    // here (e.g. the recipients query) would otherwise vanish with the discarded task.
    // Text is written in each recipient's PreferredLanguage (rama 23): one payload per language.
    // A pack question goes out in each recipient's language too (rama 24).
    public Task SendNewQuestionAsync(
        int groupId,
        string groupName,
        string questionText,
        IReadOnlyDictionary<string, string> textTranslations,
        DuelPush? duel
    ) =>
        RunLoggedAsync("new_question", groupId, async context =>
        {
            var recipients = await GetEligibleMembersAsync(context, groupId, excludeUserId: null, pref => pref.NewQuestion);
            var (duelists, others) = SplitDuelists(recipients, duel);
            await SendLocalizedAsync(context, others, QuestionTtl, lang =>
            {
                var text = ContentTranslations.Pick(questionText, textTranslations, lang);
                return BuildPayload($"❓ {groupName}", Shorten(text), groupId, groupName, "new_question", lang);
            });
            if (duel != null)
                await SendDuelAsync(context, duelists, duel, QuestionTtl, groupId, groupName, "duel",
                    lang => ContentTranslations.Pick(questionText, textTranslations, lang));
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

    public Task SendRaceToSelectAsync(int groupId, int selectorUserId, string groupName, TimeOnly dailyTime) =>
        RunLoggedAsync("race_to_select", groupId, async context =>
        {
            var recipients = await GetEligibleMembersAsync(context, groupId, excludeUserId: selectorUserId, pref => pref.RaceToSelect);

            // Only members who have already voted today's open question: they're the ones
            // playing today, so nobody else gets an extra push (developer, rama 40).
            var openQuestionId = await context.DailyEntries
                .Where(d => d.GroupId == groupId && d.ActivatedAt != null)
                .OrderByDescending(d => d.Date)
                .Select(d => (int?)d.QuestionId)
                .FirstOrDefaultAsync();
            var voterIds = await context.Votes
                .Where(v => v.QuestionId == openQuestionId)
                .Select(v => v.UserId)
                .Distinct()
                .ToListAsync();
            recipients = recipients.Where(r => voterIds.Contains(r.Subscription.UserId)).ToList();

            await SendLocalizedAsync(context, recipients, UntilNextDailyTime(dailyTime), lang =>
                BuildPayload(
                    ServerText.Push.RaceToSelectTitle(lang, groupName),
                    ServerText.Push.RaceToSelectBody(lang, CoinRewards.RaceWon),
                    groupId, groupName, "race_to_select", lang
                ));
        });

    public Task SendFlashQuestionAsync(
        int groupId, int launcherUserId, string groupName, string launcherUsername, string questionText, DuelPush? duel,
        bool anonymous = false, bool guessLauncher = false) =>
        RunLoggedAsync("flash_question", groupId, async context =>
        {
            var recipients = await GetEligibleMembersAsync(context, groupId, excludeUserId: launcherUserId, pref => pref.FlashQuestion);
            var (duelists, others) = SplitDuelists(recipients, duel);
            var text = Shorten(questionText);
            var named = !anonymous && !guessLauncher;
            await SendLocalizedAsync(context, others, FlashService.OpenFor, lang =>
                BuildPayload(
                    ServerText.Push.FlashQuestionTitle(lang, groupName),
                    anonymous ? ServerText.Push.FlashQuestionAnonymousBody(lang, text)
                    : guessLauncher ? ServerText.Push.FlashQuestionGuessBody(lang, text)
                    : ServerText.Push.FlashQuestionBody(lang, launcherUsername, text),
                    groupId, groupName, "flash_question", lang, actor: named ? launcherUsername : null
                ));
            if (duel != null)
                await SendDuelAsync(context, duelists, duel, FlashService.OpenFor, groupId, groupName, "flash_duel", _ => questionText);
        });

    // Opens the history straight on the capsule. Follows the "Preguntas flash" preference.
    public Task SendCapsuleOpenedAsync(
        int groupId, int flashId, string groupName, string? launcherUsername, DateOnly launchedOn, string questionText) =>
        RunLoggedAsync("flash_capsule_opened", groupId, async context =>
        {
            var recipients = await GetEligibleMembersAsync(context, groupId, excludeUserId: null, pref => pref.FlashQuestion);
            var text = Shorten(questionText);
            var date = launchedOn.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            await SendLocalizedAsync(context, recipients, SummaryTtl, lang =>
                BuildPayload(
                    ServerText.Push.CapsuleOpenedTitle(lang, groupName),
                    launcherUsername == null
                        ? ServerText.Push.CapsuleOpenedAnonymousBody(lang, date, text)
                        : ServerText.Push.CapsuleOpenedBody(lang, launcherUsername, date, text),
                    groupId, groupName, "flash_capsule_opened", lang,
                    url: $"/groups/{groupId}/history?flash={flashId}"
                ));
        });

    // Opens the group page straight on that month's story.
    // Not tied to a group: groupId 0 keeps it apart from every group's merged notification.
    public Task SendSeasonalItemAsync(string itemKey, int price, int daysLeft) =>
        RunLoggedAsync("seasonal_item", 0, async context =>
        {
            var optedOut = context.NotificationPreferences.Where(p => !p.ShopNews).Select(p => p.UserId);
            var recipients = await context.DevicePushSubscriptions
                .Where(s => !optedOut.Contains(s.UserId))
                .Select(s => new Recipient(s, s.User.PreferredLanguage))
                .ToListAsync();
            await SendLocalizedAsync(context, recipients, TimeSpan.FromDays(daysLeft), lang =>
                BuildPayload(
                    ServerText.Push.SeasonalTitle(lang, itemKey),
                    ServerText.Push.SeasonalBody(lang, itemKey, price, daysLeft),
                    0, "Vaya Preguntita", "shop_news", lang, url: "/shop?tab=style"
                ));
        });

    public Task SendMonthlySummaryAsync(int groupId, string groupName, DateOnly month) =>
        RunLoggedAsync("monthly_summary", groupId, async context =>
        {
            var recipients = await GetEligibleMembersAsync(context, groupId, excludeUserId: null, pref => pref.MonthlySummary);
            var url = $"/groups/{groupId}?summary={month.ToString("yyyy-MM", CultureInfo.InvariantCulture)}";
            await SendLocalizedAsync(context, recipients, SummaryTtl, lang =>
                BuildPayload(
                    ServerText.Push.MonthlySummaryTitle(lang, groupName),
                    ServerText.Push.MonthlySummaryBody(lang, month.Month, groupName),
                    groupId, groupName, "monthly_summary", lang, url: url
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

    public Task SendNewMessageAsync(
        int groupId, int questionId, int senderUserId, string groupName, string senderUsername, string messagePreview) =>
        RunLoggedAsync("new_message", groupId, async context =>
        {
            var eligible = await GetEligibleMembersAsync(context, groupId, excludeUserId: senderUserId, pref => pref.NewMessage);
            // Rama 49: members who haven't voted can't read the chat yet; their pushes are dropped.
            var voterIds = await context.Votes
                .Where(v => v.QuestionId == questionId)
                .Select(v => v.UserId)
                .Distinct()
                .ToListAsync();
            var recipients = eligible.Where(r => voterIds.Contains(r.Subscription.UserId)).ToList();
            var preview = messagePreview.Length > 80 ? messagePreview[..77] + "…" : messagePreview;
            await SendLocalizedAsync(context, recipients, MessageTtl, lang =>
                BuildPayload($"💬 {groupName}", $"{senderUsername}: {preview}", groupId, groupName, "new_message", lang, actor: senderUsername));
        });

    public Task SendStreakDangerAsync(
        int groupId, int userId, string groupName, int streak, TimeOnly dailyTime, bool freezeCovers = false) =>
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
                    freezeCovers
                        ? ServerText.Push.StreakDangerFrozenBody(lang, streak, groupName, time)
                        : ServerText.Push.StreakDangerBody(lang, streak, groupName, time),
                    groupId, groupName, "streak_danger", lang
                ));
        });

    public Task SendNudgeAsync(
        int groupId, int targetUserId, string groupName, string senderUsername, string? message, TimeSpan ttl) =>
        RunLoggedAsync("nudge", groupId, async context =>
        {
            var recipients = await context.DevicePushSubscriptions
                .Where(s => s.UserId == targetUserId)
                .Select(s => new Recipient(s, s.User.PreferredLanguage))
                .ToListAsync();
            await SendLocalizedAsync(context, recipients, ttl < TimeSpan.FromMinutes(5) ? TimeSpan.FromMinutes(5) : ttl, lang =>
                BuildPayload(
                    ServerText.Push.NudgeTitle(lang, groupName),
                    message is null
                        ? ServerText.Push.NudgeBody(lang, senderUsername)
                        : ServerText.Push.NudgeBodyWithMessage(lang, senderUsername, message),
                    groupId, groupName, "nudge", lang, actor: senderUsername
                ));
        });

    public Task SendReactionAsync(
        int groupId, int authorUserId, string groupName, string reactorUsername, string emoji,
        bool isAnswer, string preview, string url) =>
        RunLoggedAsync("reaction", groupId, async context =>
        {
            var recipients = await GetEligibleMembersAsync(context, groupId, excludeUserId: null, pref => pref.Reaction);
            recipients = recipients.Where(r => r.Subscription.UserId == authorUserId).ToList();
            var text = Shorten(preview);
            await SendLocalizedAsync(context, recipients, MessageTtl, lang =>
                BuildPayload(
                    ServerText.Push.ReactionTitle(lang, emoji, groupName),
                    isAnswer
                        ? ServerText.Push.ReactionToAnswerBody(lang, reactorUsername, emoji, text)
                        : ServerText.Push.ReactionToMessageBody(lang, reactorUsername, emoji, text),
                    groupId, groupName, "reaction", lang, actor: reactorUsername, url: url
                ));
        });

    private sealed record Recipient(DevicePushSubscription Subscription, string Language);

    private static string Shorten(string text) => text.Length > 80 ? text[..77] + "…" : text;

    // The duel's sides (but not its chooser, who gets the normal push or none) are taken out
    // of the eligible recipients, so they get the duel push in its place.
    private static (List<Recipient> Duelists, List<Recipient> Others) SplitDuelists(
        List<Recipient> recipients, DuelPush? duel)
    {
        if (duel == null)
            return ([], recipients);
        bool IsDuelist(Recipient r) =>
            duel.Involves(r.Subscription.UserId) && r.Subscription.UserId != duel.ChooserUserId;
        return (recipients.Where(IsDuelist).ToList(), recipients.Where(r => !IsDuelist(r)).ToList());
    }

    // Kind "duel" stands in for new_question (it starts a new cycle in sw.js), "flash_duel"
    // for flash_question.
    private async Task SendDuelAsync(
        AppDbContext context,
        List<Recipient> duelists,
        DuelPush duel,
        TimeSpan ttl,
        int groupId,
        string groupName,
        string kind,
        Func<string, string> questionTextFor)
    {
        await SendLocalizedAsync(context, duelists, ttl, lang =>
        {
            var text = Shorten(questionTextFor(lang));
            return BuildPayload(
                ServerText.Push.DuelTitle(lang, groupName),
                duel.Anonymous ? ServerText.Push.DuelAnonymousBody(lang, text)
                    : duel.ChooserIsDuelist
                        ? ServerText.Push.DuelChallengedBody(lang, duel.ChooserUsername, text)
                        : ServerText.Push.DuelPittedBody(lang, duel.ChooserUsername, text),
                groupId, groupName, kind, lang, actor: duel.Anonymous ? null : duel.ChooserUsername
            );
        });
    }

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

    private static string BuildPayload(string title, string body, int groupId, string groupName, string kind, string lang, string? actor = null, string? url = null)
    {
        url ??= $"/groups/{groupId}";
        return JsonSerializer.Serialize(new
        {
            notification = new
            {
                title,
                body,
                icon = IconPath,
                badge = BadgePath,
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
