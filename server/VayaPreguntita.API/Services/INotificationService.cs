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
    Task SendUserVotedAsync(int groupId, int voterUserId, string groupName, string voterUsername);
    Task SendNewMessageAsync(int groupId, int senderUserId, string groupName, string senderUsername, string messagePreview);
    // Caller has already checked the user's StreakDanger preference, lead time and group mute.
    // freezeCovers: a held freeze (§17.9) will save the streak if the member misses the cycle.
    Task SendStreakDangerAsync(
        int groupId, int userId, string groupName, int streak, TimeOnly dailyTime, bool freezeCovers = false);
}
