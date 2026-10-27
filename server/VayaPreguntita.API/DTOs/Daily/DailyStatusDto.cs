// DTOs/Daily/DailyStatusDto.cs
// What GET /daily/current returns. It surfaces BOTH coexisting states (§4.7):
//   - today:     the currently-open question (voting / results), if any.
//   - selection: the next-day question being chosen (the "te toca elegir" panel).

using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.DTOs.Streaks;

namespace VayaPreguntita.API.DTOs.Daily;

public class DailyStatusDto
{
    public TodayStateDto Today { get; set; } = new();

    // null before the group has started a cycle (< 2 members), and in the brief window
    // after T before the tick draws the next selector (§17.4).
    public SelectionStateDto? Selection { get; set; }

    // The current user's streak in this group (rama 19).
    public MyStreakDto MyStreak { get; set; } = new();

    // Open flash questions (§17.11), oldest first; empty when there are none.
    public List<FlashStateDto> Flashes { get; set; } = [];
}

public class TodayStateDto
{
    // "voting" | "results" | "no_question"
    public string Status { get; set; } = "no_question";

    // The currently-open question (null when status is "no_question").
    public QuestionToVoteDto? Question { get; set; }

    public bool UserHasVoted { get; set; }

    // Present only when UserHasVoted.
    public QuestionResultDto? Results { get; set; }

    // Next T (UTC) — when the open question closes. null when no_question.
    public DateTime? ClosesAt { get; set; }

    // Members who have voted the open question (no answers), and the group's size, for
    // the "3/6 han votado" row.
    public List<int> VoterIds { get; set; } = [];
    public int MemberCount { get; set; }

    // The guess (§17.6, §17.17), once you have voted; null when the question isn't guessable.
    public AuthorGuessDto? Guess { get; set; }

    // An automatic pick nobody chose (§17.17): «Hoy decidió el azar» where the guess would be.
    public bool DecidedByChance { get; set; }

    // You voted and haven't changed your vote yet (rama 43, §17.10).
    public bool CanChangeVote { get; set; }
}

public class SelectionStateDto
{
    // The day being selected (the next activation date).
    public DateOnly Date { get; set; }

    // When the pending question will activate (next T, UTC).
    public DateTime ActivatesAt { get; set; }

    // Only sent to the selector themself (null for everyone else, §17.6).
    public int? SelectorUserId { get; set; }
    public string? SelectorUsername { get; set; }

    // Drives the "te toca elegir" indicator.
    public bool IsCurrentUserSelector { get; set; }

    // The pending (next-day) question — exposed ONLY to the selector, to keep
    // the surprise for everyone else until it activates at T. null otherwise.
    public QuestionToVoteDto? PendingQuestion { get; set; }

    // True while the pending question is still the automatic pick (SelectionKind.Auto):
    // the selector hasn't picked, created or confirmed one yet (§17.4).
    public bool IsAutoSelected { get; set; }

    // From T − 10 min while still auto-selected (§17.5): any member may choose ("¡Elige tú!"),
    // so PendingQuestion is sent to everyone.
    public bool RaceOpen { get; set; }
}
