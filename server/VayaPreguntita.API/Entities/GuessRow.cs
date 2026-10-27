namespace VayaPreguntita.API.Entities;

// The guess one member makes on one question (§17.6, §17.17): who wrote it, who chose it or
// who launched it. Shared by the daily guess (AuthorGuess) and the flash guess (FlashGuess);
// not mapped on its own, each table keeps its own key.
public abstract class GuessRow
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Who the member named. null with GuessedAt set = skipped ("Saltar").
    public int? GuessedUserId { get; set; }
    public User? GuessedUser { get; set; }

    // Frozen when the guess is made, so later edits to the question can't flip it.
    public bool IsCorrect { get; set; }

    // null while the guess is still pending (e.g. the member only bought the hint).
    public DateTime? GuessedAt { get; set; }

    // The hint (§17.10) was bought for this question.
    public bool HintUsed { get; set; }
}
