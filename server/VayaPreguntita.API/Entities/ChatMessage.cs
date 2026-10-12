namespace VayaPreguntita.API.Entities;

public class ChatMessage
{
    public int Id { get; set; }
    public string Body { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int DailyEntryId { get; set; }
    public DailyEntry DailyEntry { get; set; } = null!;

    // null => automatic system message (e.g. streak milestones)
    public int? UserId { get; set; }
    public User? User { get; set; }

    // Streak system messages keep what they are about, so each reader gets the message in
    // their own language (rama 24); Body holds the Spanish text. Null on user messages and on
    // system messages written before rama 24.
    public string? StreakUsername { get; set; }
    public int? StreakDays { get; set; }
}
