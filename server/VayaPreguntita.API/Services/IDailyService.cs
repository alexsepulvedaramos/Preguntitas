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
    Task<VoteResult> VoteAsync(int groupId, int userId, CreateVoteDto dto);
    Task PreselectForGroupAsync(int groupId, DateOnly date);
}
