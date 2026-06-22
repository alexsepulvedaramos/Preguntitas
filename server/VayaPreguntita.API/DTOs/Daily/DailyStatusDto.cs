// DTOs/Daily/DailyStatusDto.cs
// What GET /daily/current returns. It surfaces BOTH coexisting states (§4.7):
//   - today:     the currently-open question (voting / results), if any.
//   - selection: the next-day question being chosen (the "te toca elegir" panel).

using VayaPreguntita.API.DTOs.Questions;

namespace VayaPreguntita.API.DTOs.Daily;

public class DailyStatusDto
{
    public TodayStateDto Today { get; set; } = new();

    // null before the group has started a cycle (< 2 members).
    public SelectionStateDto? Selection { get; set; }
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
}

public class SelectionStateDto
{
    // The day being selected (the next activation date).
    public DateOnly Date { get; set; }

    // When the pending question will activate (next T, UTC).
    public DateTime ActivatesAt { get; set; }

    public int SelectorUserId { get; set; }
    public string SelectorUsername { get; set; } = string.Empty;

    // Drives the "te toca elegir" indicator.
    public bool IsCurrentUserSelector { get; set; }

    // The pending (next-day) question — exposed ONLY to the selector, to keep
    // the surprise for everyone else until it activates at T. null otherwise.
    public QuestionToVoteDto? PendingQuestion { get; set; }

    public bool IsAutoSelected { get; set; }
}
