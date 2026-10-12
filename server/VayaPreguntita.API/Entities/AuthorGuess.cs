namespace VayaPreguntita.API.Entities;

// "Guess the author" (§17.6): one row per member and daily entry. A Vote can span several
// rows (multi-select polls, secret pairings), so the guess lives in its own table.
public class AuthorGuess
{
    public int DailyEntryId { get; set; }
    public DailyEntry DailyEntry { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Who the member said wrote the question. null with GuessedAt set = skipped ("Saltar").
    public int? GuessedCreatorId { get; set; }
    public User? GuessedCreator { get; set; }

    // Frozen when the guess is made, so later edits to the question can't flip it.
    public bool IsCorrect { get; set; }

    // null while the guess is still pending (e.g. the member only bought the hint).
    public DateTime? GuessedAt { get; set; }

    // The author hint (§17.10) was bought for this question.
    public bool HintUsed { get; set; }
}
