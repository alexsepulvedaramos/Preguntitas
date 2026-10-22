using VayaPreguntita.API.DTOs.Questions;

namespace VayaPreguntita.API.DTOs.Daily;

// An open flash question (§17.11), as folded into GET daily/current (`flashes`) and returned
// by POST flash / flash/{id}/vote.
public class FlashStateDto
{
    public int Id { get; set; }
    public QuestionToVoteDto Question { get; set; } = null!;

    // Who bought it: the author is public on a flash question — null on an anonymous one.
    public VoterDto? LaunchedBy { get; set; }

    public DateTime ActivatedAt { get; set; }
    public DateTime ClosesAt { get; set; }

    public bool UserHasVoted { get; set; }

    // Present only when UserHasVoted (same rule as the daily question) and it isn't a sealed
    // time capsule.
    public QuestionResultDto? Results { get; set; }

    // How many members have voted it (also on an anonymous flash, which hides who).
    public int VoteCount { get; set; }

    // Members who have voted it (no answers). Always empty on an anonymous flash.
    public List<int> VoterIds { get; set; } = [];

    // You voted and haven't changed your vote yet (rama 43, §17.10).
    public bool CanChangeVote { get; set; }

    // Flash variants (rama 50, §17.15 item 6).
    // launchedBy is null with AnonymousAuthor; VoterIds is empty and the results name no
    // voters with AnonymousVotes.
    public bool AnonymousAuthor { get; set; }
    public bool AnonymousVotes { get; set; }

    // Time capsule: when its results open (null on a normal flash); Sealed until then.
    public DateTime? CapsuleOpensAt { get; set; }
    public bool Sealed { get; set; }

    // +24 h already bought (once per flash); ExtendedBy is null on an anonymous flash.
    public bool Extended { get; set; }
    public VoterDto? ExtendedBy { get; set; }
}

// POST flash/{id}/extend: the extended flash and the buyer's balance after paying.
public class ExtendFlashResponseDto
{
    public FlashStateDto Flash { get; set; } = null!;
    public int Balance { get; set; }
}

// Launch add-ons (rama 50), sent as query parameters next to ?useOwned.
public record FlashOptions(bool AnonymousAuthor = false, bool AnonymousVotes = false, DateOnly? CapsuleOpensOn = null);

// POST flash: the new flash and the buyer's balance after paying.
public class LaunchFlashResponseDto
{
    public FlashStateDto Flash { get; set; } = null!;
    public int Balance { get; set; }
    public Coins.InventoryDto Inventory { get; set; } = new();
}
