namespace VayaPreguntita.API.Services;

public interface INotificationService
{
    Task SendNewQuestionAsync(
        int groupId,
        string groupName,
        string questionText,
        IReadOnlyDictionary<string, string> textTranslations
    );
    Task SendSelectorTurnAsync(int groupId, int selectorUserId, string groupName, TimeOnly dailyTime);
    // "¡Elige tú!" (§17.5): every member but the selector, 10 minutes before T.
    Task SendRaceToSelectAsync(int groupId, int selectorUserId, string groupName, TimeOnly dailyTime);
    // "⚡ Pregunta flash" (§17.11): every member but the buyer.
    Task SendFlashQuestionAsync(int groupId, int launcherUserId, string groupName, string launcherUsername, string questionText);
    // "Resumen del mes" (§17.12): every member, when last month's summary is ready.
    Task SendMonthlySummaryAsync(int groupId, string groupName, DateOnly month);
    Task SendUserVotedAsync(int groupId, int voterUserId, string groupName, string voterUsername);
    Task SendNewMessageAsync(int groupId, int senderUserId, string groupName, string senderUsername, string messagePreview);
    // Caller has already checked the user's StreakDanger preference, lead time and group mute.
    // freezeCovers: a held freeze (§17.9) will save the streak if the member misses the cycle.
    Task SendStreakDangerAsync(
        int groupId, int userId, string groupName, int streak, TimeOnly dailyTime, bool freezeCovers = false);
}
