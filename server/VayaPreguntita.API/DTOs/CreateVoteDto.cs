namespace VayaPreguntita.API.DTOs;

public class CreateVoteDto
{
    public int QuestionId { get; set; }

    // Gamification guess
    public int? GuessedCreatorId { get; set; }

    // --- The 4 flexible response types ---
    public int? SelectedOptionId { get; set; }
    public int? SelectedTargetUserId { get; set; } // Added for user voting
    public int? NumericValue { get; set; } // Added for the scale
    public string? FreeText { get; set; }
}
