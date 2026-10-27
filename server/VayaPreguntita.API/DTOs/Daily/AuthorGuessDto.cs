using VayaPreguntita.API.DTOs.Coins;
using VayaPreguntita.API.DTOs.Questions;

namespace VayaPreguntita.API.DTOs.Daily;

// The guess (§17.6, §17.17) as the current member sees it, once they have voted: who wrote it,
// who chose it or who launched the flash. Absent when the question isn't guessable.
public class AuthorGuessDto
{
    // "author" (¿Quién la escribió?) | "chooser" (¿Quién la eligió?) | "launcher" (¿Quién la lanzó?)
    public string Kind { get; set; } = "author";

    public const string Pending = "pending";
    public const string Guessed = "guessed";
    public const string Skipped = "skipped";
    public const string Mine = "mine";

    // "pending" | "guessed" | "skipped" | "mine" (you are the one to guess)
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

    // Guessed, skipped and mine: who wrote, chose or launched it.
    public VoterDto? Author { get; set; }

    // Mine only: "Te han pillado X de Y" — members who guessed right / members who guessed.
    public int CaughtCount { get; set; }
    public int GuessCount { get; set; }
}

// POST daily/guess and flash/{id}/guess: CreatorId (the member named), or Skip to give up and
// see the answer.
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

// POST daily/guess/hint and flash/{id}/guess/hint
public class AuthorHintResponseDto
{
    public AuthorGuessDto Guess { get; set; } = new();
    public int Balance { get; set; }
    public Coins.InventoryDto Inventory { get; set; } = new();
}
