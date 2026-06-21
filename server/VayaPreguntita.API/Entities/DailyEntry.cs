namespace VayaPreguntita.API.Entities;

public class DailyEntry
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public bool IsAutoSelected { get; set; } = false;
    public DateTime PreselectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ActivatedAt { get; set; }

    // ==========================================
    // FOREIGN KEYS & NAVIGATION PROPERTIES
    // ==========================================
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    // The selected question for that day (the selector can change it until activation)
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    // Who is the selector for that day?
    public int SelectorUserId { get; set; }
    public User Selector { get; set; } = null!;
}
