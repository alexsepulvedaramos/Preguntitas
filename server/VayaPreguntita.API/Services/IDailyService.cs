// Services/IDailyService.cs
using VayaPreguntita.API.DTOs.Coins;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Services;

public interface IDailyService
{
    Task<DailyStatusDto> GetCurrentStatusAsync(int groupId, int userId);
    // Coins is set only when the caller won the race to select (§17.5).
    Task<(SelectResult Result, CoinsEarnedDto? Coins)> SelectQuestionAsync(
        int groupId,
        int userId,
        SelectQuestionDto dto
    );
    Task<(SelectResult Result, CoinsEarnedDto? Coins)> ConfirmSelectionAsync(int groupId, int userId);
    Task<(VoteResult result, VoteResponseDto? response)> VoteAsync(
        int groupId,
        int userId,
        CreateVoteDto dto
    );
    // Late vote on a closed question from the history (§17.7): no streak, coins or push.
    Task<VoteResult> LateVoteAsync(int groupId, int userId, DateOnly date, CreateVoteDto dto);
    // Returns the selectorUserId if a new entry was created, null if it already existed (idempotent).
    Task<int?> PreselectForGroupAsync(int groupId, DateOnly date, bool activateImmediately = false);
}
