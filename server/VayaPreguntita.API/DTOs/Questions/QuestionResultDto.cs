// QuestionResultDto.cs
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.DTOs.Questions;

public class QuestionResultDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public DateTime DateCreated { get; set; }
    public int TotalVotes { get; set; }
    public List<OptionResultDto> Results { get; set; } = [];

    // OpenText answers, and CustomPoll "Otro" free-text answers.
    public List<FreeTextResponseDto> FreeTextResponses { get; set; } = [];

    // Scale only — needed by the frontend to render the full distribution range.
    public int? RangeMin { get; set; }
    public int? RangeMax { get; set; }
}

public class FreeTextResponseDto
{
    // Author, so the answer bubble can show their avatar and streak frame (rama 19).
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? FrameColor { get; set; }
    public string Text { get; set; } = string.Empty;
}
