using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs.Questions;

public class HistoryEntryDto
{
    public DateOnly Date { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public int TotalVotes { get; set; }

    // Member-written questions only (§17.6): who wrote it, and whether your guess was right
    // (null when you didn't guess, or skipped).
    public string? AuthorUsername { get; set; }
    public bool? MyGuessCorrect { get; set; }

    // Set on a closed flash question (§17.11, marked ⚡): open it by this id, not by date.
    // AuthorUsername is then the member who launched it.
    public int? FlashId { get; set; }

    // Flash variants (rama 50): an anonymous flash has no AuthorUsername; a time capsule
    // carries when it opens.
    public bool IsAnonymous { get; set; }
    public DateTime? CapsuleOpensAt { get; set; }
}

public class HistoryPageDto
{
    public List<HistoryEntryDto> Items { get; set; } = [];
    public bool HasMore { get; set; }
}
