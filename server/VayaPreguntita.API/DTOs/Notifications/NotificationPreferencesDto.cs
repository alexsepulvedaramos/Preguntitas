namespace VayaPreguntita.API.DTOs.Notifications;

public record NotificationPreferencesDto(
    bool NewQuestion,
    bool SelectorTurn,
    bool UserVoted,
    bool NewMessage,
    bool StreakDanger = true,
    // Hours before the cycle closes; one of NotificationPreferencesDto.AllowedStreakDangerHours.
    int StreakDangerHoursBefore = 3,
    // "¡Elige tú!" — the race to select (§17.5).
    bool RaceToSelect = true,
    // "⚡ Pregunta flash" (§17.11).
    bool FlashQuestion = true
)
{
    public static readonly int[] AllowedStreakDangerHours = [1, 2, 3, 4, 6, 8];
}
