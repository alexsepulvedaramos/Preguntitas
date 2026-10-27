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
    bool FlashQuestion = true,
    // "Resumen del mes" (§17.12).
    bool MonthlySummary = true,
    // "👉 Toque" (§17.10).
    bool Nudge = true,
    // "😂 Reacción" (§17.15 item 6).
    bool Reaction = true,
    // "🎃 Novedad en la tienda" — seasonal frames (rama 57).
    bool ShopNews = true
)
{
    public static readonly int[] AllowedStreakDangerHours = [1, 2, 3, 4, 6, 8];
}
