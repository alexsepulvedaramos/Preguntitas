namespace VayaPreguntita.API.Services;

public interface INotificationService
{
    Task SendNewQuestionAsync(
        int groupId,
        string groupName,
        string questionText,
        IReadOnlyDictionary<string, string> textTranslations,
        DuelPush? duel
    );
    Task SendSelectorTurnAsync(int groupId, int selectorUserId, string groupName, TimeOnly dailyTime);
    // "¡Elige tú!" (§17.5): every member but the selector, 10 minutes before T.
    Task SendRaceToSelectAsync(int groupId, int selectorUserId, string groupName, TimeOnly dailyTime);
    // "⚡ Pregunta flash" (§17.11): every member but the buyer. A duel's two sides get the duel
    // push instead (§17.15 item 6).
    Task SendFlashQuestionAsync(
        int groupId, int launcherUserId, string groupName, string launcherUsername, string questionText, DuelPush? duel);
    // "Resumen del mes" (§17.12): every member, when last month's summary is ready.
    Task SendMonthlySummaryAsync(int groupId, string groupName, DateOnly month);
    Task SendUserVotedAsync(int groupId, int voterUserId, string groupName, string voterUsername);
    Task SendNewMessageAsync(int groupId, int senderUserId, string groupName, string senderUsername, string messagePreview);
    // Caller has already checked the user's StreakDanger preference, lead time and group mute.
    // freezeCovers: a held freeze (§17.9) will save the streak if the member misses the cycle.
    Task SendStreakDangerAsync(
        int groupId, int userId, string groupName, int streak, TimeOnly dailyTime, bool freezeCovers = false);
    // Toque / nudge (rama 43): caller has already checked that it can reach the target (a device,
    // the Nudge preference, the group not muted). ttl = until the question closes.
    Task SendNudgeAsync(
        int groupId, int targetUserId, string groupName, string senderUsername, string? message, TimeSpan ttl);
    // Reaction (rama 48): to the author of the message or answer, if their Reaction preference
    // is on and the group isn't muted. The caller applies ReactionRules.PushCooldown.
    Task SendReactionAsync(
        int groupId, int authorUserId, string groupName, string reactorUsername, string emoji,
        bool isAnswer, string preview, string url);
}

// A duel (§17.15 item 6) replaces the normal question push for its two sides, except the one
// who set it up: "Ana te ha retado a un duelo" / "Ana os ha puesto a pelear". It follows the
// preference of the push it replaces and the group mute.
public sealed record DuelPush(int ChooserUserId, string ChooserUsername, int FirstUserId, int SecondUserId)
{
    public bool Involves(int userId) => userId == FirstUserId || userId == SecondUserId;

    // The chooser is one side: the other one is being challenged.
    public bool ChooserIsDuelist => Involves(ChooserUserId);
}
