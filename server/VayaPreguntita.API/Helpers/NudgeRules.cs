namespace VayaPreguntita.API.Helpers;

// Toque / nudge rules (rama 43, §17.10).
public static class NudgeRules
{
    // The optional message after the reminder ("Vótame a mí y te invito a una cerve").
    public const int MaxMessageLength = 80;

    // A member receives at most one nudge per group in this window, from anyone.
    public static readonly TimeSpan Cooldown = TimeSpan.FromHours(24);
}
