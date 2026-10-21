namespace VayaPreguntita.API.Entities;

public class NotificationPreferences
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public bool NewQuestion { get; set; } = true;
    public bool SelectorTurn { get; set; } = true;
    public bool UserVoted { get; set; } = true;
    public bool NewMessage { get; set; } = true;
    public bool StreakDanger { get; set; } = true;
    public int StreakDangerHoursBefore { get; set; } = 3;

    // "¡Elige tú!" — the selector hasn't chosen 10 minutes before T (§17.5).
    public bool RaceToSelect { get; set; } = true;

    // "⚡ Pregunta flash" — a member launched a flash question (§17.11).
    public bool FlashQuestion { get; set; } = true;

    // "Resumen del mes" — last month's summary is ready, on the 1st at T (§17.12).
    public bool MonthlySummary { get; set; } = true;

    // "👉 Toque" — a member reminds you to vote (§17.10). Off = nobody can nudge you.
    public bool Nudge { get; set; } = true;

    // "😂 Reacción" — someone reacted to your chat message or answer (§17.15 item 6).
    public bool Reaction { get; set; } = true;
}
