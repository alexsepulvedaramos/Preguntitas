namespace VayaPreguntita.API.DTOs;

public class CreateVoteDto
{
    public int QuestionId { get; set; }

    // Gamification guess
    public int? GuessedCreatorId { get; set; }

    // --- The 4 flexible response types ---
    public List<int>? SelectedOptionIds { get; set; }
    public int? SelectedTargetUserId { get; set; }
    public int? NumericValue { get; set; }
    public string? FreeText { get; set; }
}
