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
}
