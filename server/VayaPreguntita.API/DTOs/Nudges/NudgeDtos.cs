using VayaPreguntita.API.DTOs.Coins;

namespace VayaPreguntita.API.DTOs.Nudges;

// GET nudges/targets: each member who hasn't voted the question yet (you excluded), and
// whether a nudge can reach them right now.
public class NudgeTargetDto
{
    public const string Available = "available";
    public const string Recent = "recent";
    public const string Unreachable = "unreachable";

    public int UserId { get; set; }

    // "available" | "recent" (nudged in this group in the last 24 h) | "unreachable"
    // (notifications off, nudges disabled or the group muted — not told apart, on purpose).
    public string Status { get; set; } = Available;
}

// POST nudges. FlashId null = today's question.
public class SendNudgeRequestDto
{
    public int TargetUserId { get; set; }
    public int? FlashId { get; set; }
    public string? Message { get; set; }

    // Uses one from the inventory instead of paying (rama 45).
    public bool UseOwned { get; set; }
}

public class SendNudgeResponseDto
{
    public int Balance { get; set; }
    public InventoryDto Inventory { get; set; } = new();
}
