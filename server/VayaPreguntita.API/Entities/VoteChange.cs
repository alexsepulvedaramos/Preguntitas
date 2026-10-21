namespace VayaPreguntita.API.Entities;

// "Change my vote" (rama 43, §17.10): a member redid their vote on a question. One per member
// and question, so the key itself enforces "once per question" — whether it was paid with
// coins or with an item from the inventory (which writes no ledger row).
public class VoteChange
{
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime ChangedAt { get; set; }
}
