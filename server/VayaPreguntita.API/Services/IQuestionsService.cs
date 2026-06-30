// Services/IQuestionsService.cs

namespace VayaPreguntita.API.Services;

using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Enums;

public interface IQuestionsService
{
    // Paginated, cursor-based (by Question.Id, which is monotonic and already implies
    // insertion order) so the picker can page through a group's pool without loading it
    // all at once. `type` optionally narrows to one QuestionType.
    Task<QuestionPageDto> GetPoolAsync(
        int groupId,
        QuestionType? type = null,
        int? before = null,
        int pageSize = 12
    );
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

    // Paginated past entries for the group's history view. Cursor-based: pass the last
    // seen date as `before` to get the next page. Returns at most `pageSize` items,
    // plus a `HasMore` flag.
    Task<HistoryPageDto> GetHistoryAsync(int groupId, DateOnly? before, int pageSize = 20);
}
