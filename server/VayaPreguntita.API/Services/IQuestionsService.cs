// Services/IQuestionsService.cs

namespace VayaPreguntita.API.Services;

using VayaPreguntita.API.DTOs.Questions;

public interface IQuestionsService
{
    Task<IEnumerable<QuestionDto>> GetPoolAsync(int groupId);
    Task<QuestionResultDto?> GetByDateAsync(int groupId, DateOnly date);

    // Returns the created question, or an error message when membership validation fails (§9).
    Task<(QuestionDto? question, string? error)> CreateAsync(
        int groupId,
        int creatorId,
        CreateQuestionDto dto
    );

    // Removes an unused pool question. Returns an error code: "not_found", "in_use",
    // "forbidden", or null on success.
    Task<string?> DeleteAsync(int groupId, int userId, int questionId);
}
