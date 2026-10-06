namespace VayaPreguntita.API.DTOs.Notifications;

public record NotificationPreferencesDto(
    bool NewQuestion,
    bool SelectorTurn,
    bool UserVoted,
    bool NewMessage,
    bool StreakDanger = true,
    // Hours before the cycle closes; one of NotificationPreferencesDto.AllowedStreakDangerHours.
    int StreakDangerHoursBefore = 3
)
{
    public static readonly int[] AllowedStreakDangerHours = [1, 2, 3, 4, 6, 8];
}
