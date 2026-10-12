using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Entities;

// Append-only coin ledger (§17.2). Written in the same DB transaction as User.Coins.
public class CoinTransaction
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Positive when earning, negative when spending.
    public int Amount { get; set; }
    public CoinReason Reason { get; set; }

    // The group the coins came from, when there is one (null once the group is deleted).
    public int? GroupId { get; set; }
    public Group? Group { get; set; }

    // What the transaction refers to (e.g. the DailyEntry voted on). Together with UserId
    // and Reason it makes every reward idempotent.
    public int? RefId { get; set; }

    public DateTime CreatedAt { get; set; }
}
