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
}
