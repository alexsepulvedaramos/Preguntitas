namespace VayaPreguntita.API.Entities;

// Flash question (§17.11): an extra question a member buys and launches at once. It lives
// next to the daily one for 24 h and never touches the daily lifecycle, streaks or coins.
public class FlashEntry
{
    public int Id { get; set; }
    public DateTime ActivatedAt { get; set; }
    public DateTime ClosesAt { get; set; }

    // Local (UTC+2) launch day: one flash per member and group per local day.
    public DateOnly LocalDate { get; set; }

    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;

    // Its own question, written by the buyer (IsUsed, so it never shows up in the pool).
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    // Public: the buyer is the author, so there is no "guess the author".
    public int LaunchedByUserId { get; set; }
    public User LaunchedBy { get; set; } = null!;
}
