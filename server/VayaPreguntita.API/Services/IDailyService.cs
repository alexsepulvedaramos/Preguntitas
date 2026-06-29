// Services/IDailyService.cs
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Services;

public interface IDailyService
{
    Task<DailyStatusDto> GetCurrentStatusAsync(int groupId, int userId);
    Task<SelectionSourcesDto> GetSelectionSourcesAsync(int groupId);
    Task<SelectResult> SelectQuestionAsync(int groupId, int userId, SelectQuestionDto dto);
    Task<(VoteResult result, QuestionResultDto? results)> VoteAsync(
        int groupId,
        int userId,
        CreateVoteDto dto
    );
    // Returns the selectorUserId if a new entry was created, null if it already existed (idempotent).
    Task<int?> PreselectForGroupAsync(int groupId, DateOnly date, bool activateImmediately = false);
}
