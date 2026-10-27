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

    // The buyer is the author. Hidden until you guess when GuessLauncher is set, never named on
    // an anonymous flash (rama 50).
    public int LaunchedByUserId { get; set; }
    public User LaunchedBy { get; set; } = null!;

    // "¿Quién la lanzó?" (§17.17): set at launch on a non-anonymous flash in a group of 3+, so
    // flashes launched before it keep showing their launcher.
    public bool GuessLauncher { get; set; }

    // Anonymous author (rama 50, §17.15 item 6): nobody is told who launched it. Its votes stay
    // public unless AnonymousVotes is on too.
    public bool AnonymousAuthor { get; set; }

    // Anonymous votes (rama 50): nobody is told who has voted or who voted what; only the
    // tallies and the vote count are shown.
    public bool AnonymousVotes { get; set; }

    // Time capsule (rama 50): the results stay hidden from everyone until this instant (1 day
    // to 1 year after launch). null on a normal flash.
    public DateTime? ResultsHiddenUntil { get; set; }

    // When the "capsule opened" push went out (once, from the minute tick).
    public DateTime? CapsuleOpenedAt { get; set; }

    // Extension (rama 50): any member may add 24 h once while it's open.
    public int? ExtendedByUserId { get; set; }
    public User? ExtendedBy { get; set; }

    public bool IsSealed(DateTime nowUtc) => ResultsHiddenUntil is DateTime until && until > nowUtc;
}
