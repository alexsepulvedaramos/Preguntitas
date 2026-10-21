using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Services;

// "Change my vote" (rama 43, §17.10): redo your vote once per question while it is open, for
// coins or with one from the inventory. Relaxes §4.6 ("the vote is final").
public interface IVoteChangeService
{
    // Today's open question. The response carries the fresh results.
    Task<(VoteChangeResult Result, ChangeVoteResponseDto? Response)> ChangeDailyAsync(
        int groupId,
        int userId,
        CreateVoteDto dto,
        bool useOwned
    );

    // An open flash question. The response carries its fresh state.
    Task<(VoteChangeResult Result, ChangeVoteResponseDto? Response)> ChangeFlashAsync(
        int groupId,
        int userId,
        int flashId,
        CreateVoteDto dto,
        bool useOwned
    );
}
