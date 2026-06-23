using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs.Questions;

public class HistoryEntryDto
{
    public DateOnly Date { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public int TotalVotes { get; set; }
}

public class HistoryPageDto
{
    public List<HistoryEntryDto> Items { get; set; } = [];
    public bool HasMore { get; set; }
}
