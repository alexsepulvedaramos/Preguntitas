namespace VayaPreguntita.API.Entities;

public class Vote
{
    public int Id { get; set; }

    // When did the user cast this vote?
    public DateTime DateResponded { get; set; } = DateTime.UtcNow;

    // Cast from the history after the question closed (§17.7): counts in the tally like any
    // other vote, but never for streaks, coins or monthly awards.
    public bool IsLate { get; set; }

    // ==========================================
    // 1. CORE RELATIONS (Who is voting and on what?)
    // ==========================================
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // ("Guess the author" lives in AuthorGuess, one row per member and entry.)

    // ==========================================
    // 2. THE FLEXIBLE ANSWER FIELDS (Polymorphic data)
    // IMPORTANT: Only ONE of these should have a value per row!
    // ==========================================

    // A) Used for 'Deathmatch' or 'Custom Poll' (Points to the Option table)
    public int? SelectedOptionId { get; set; }
    public Option? SelectedOption { get; set; }

    // B) Used for 'The Superlative' (Points to another User in the group)
    public int? SelectedTargetUserId { get; set; }
    public User? SelectedTargetUser { get; set; }

    // C) Used for 'The Scale' (Stores a number, e.g., 1 to 10)
    public int? NumericValue { get; set; }

    // D) Used for 'Free Text' (Stores open opinions)
    public string? FreeText { get; set; }
}
