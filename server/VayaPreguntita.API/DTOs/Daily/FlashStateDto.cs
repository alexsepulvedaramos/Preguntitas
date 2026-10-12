using VayaPreguntita.API.DTOs.Questions;

namespace VayaPreguntita.API.DTOs.Daily;

// An open flash question (§17.11), as folded into GET daily/current (`flashes`) and returned
// by POST flash / flash/{id}/vote.
public class FlashStateDto
{
    public int Id { get; set; }
    public QuestionToVoteDto Question { get; set; } = null!;

    // Who bought it: the author is public on a flash question.
    public VoterDto LaunchedBy { get; set; } = null!;

    public DateTime ActivatedAt { get; set; }
    public DateTime ClosesAt { get; set; }

    public bool UserHasVoted { get; set; }

    // Present only when UserHasVoted (same rule as the daily question).
    public QuestionResultDto? Results { get; set; }

    // Members who have voted it (no answers).
    public List<int> VoterIds { get; set; } = [];
}

// POST flash: the new flash and the buyer's balance after paying.
public class LaunchFlashResponseDto
{
    public FlashStateDto Flash { get; set; } = null!;
    public int Balance { get; set; }
}
