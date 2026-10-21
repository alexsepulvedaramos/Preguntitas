namespace VayaPreguntita.API.Entities;

// Toque / nudge (rama 43, §17.10): a push reminding a member to vote an open question, with an
// optional short message. A member receives at most one per group every 24 h, from anyone.
public class Nudge
{
    public int Id { get; set; }

    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    // The open question (daily or flash) the target hadn't voted.
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    public int SenderUserId { get; set; }
    public User Sender { get; set; } = null!;

    public int TargetUserId { get; set; }
    public User Target { get; set; } = null!;

    // Shown in the push after the reminder; null when the sender wrote nothing.
    public string? Message { get; set; }

    public DateTime SentAt { get; set; }
}
