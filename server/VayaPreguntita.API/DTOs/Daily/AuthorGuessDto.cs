using VayaPreguntita.API.DTOs.Coins;
using VayaPreguntita.API.DTOs.Questions;

namespace VayaPreguntita.API.DTOs.Daily;

// "Guess the author" (§17.6) as the current member sees it, once they have voted.
// Absent from daily/current when today's question isn't guessable.
public class AuthorGuessDto
{
    public const string Pending = "pending";
    public const string Guessed = "guessed";
    public const string Skipped = "skipped";
    public const string Mine = "mine";

    // "pending" | "guessed" | "skipped" | "mine" (you wrote it)
    public string State { get; set; } = Pending;

    // Pending only: the members you can pick from (after the hint, the reduced list).
    public List<int> CandidateIds { get; set; } = [];

    // Pending only: the hint removes at least one wrong candidate, and you haven't used it.
    public bool HintAvailable { get; set; }
    public bool HintUsed { get; set; }
    public int HintPrice { get; set; }

    // Guessed only.
    public int? GuessedUserId { get; set; }
    public bool? Correct { get; set; }

    // Guessed, skipped and mine: who wrote it.
    public VoterDto? Author { get; set; }

    // Mine only: "Te han pillado X de Y" — members who guessed right / members who guessed.
    public int CaughtCount { get; set; }
    public int GuessCount { get; set; }
}

// POST daily/guess: CreatorId, or Skip to give up and see the author.
public class GuessAuthorRequestDto
{
    public int? CreatorId { get; set; }
    public bool Skip { get; set; }
}

public class GuessAuthorResponseDto
{
    public AuthorGuessDto Guess { get; set; } = new();
    public CoinsEarnedDto Coins { get; set; } = new();
}

// POST daily/guess/hint
public class AuthorHintResponseDto
{
    public AuthorGuessDto Guess { get; set; } = new();
    public int Balance { get; set; }
}
