using VayaPreguntita.API.DTOs.Reactions;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Services;

// Reactions (rama 48, §17.15 item 6) on chat messages and on player-written answers.
public interface IReactionService
{
    // emoji null = remove yours. Returns the message's reactions after the change.
    Task<(ReactionResult Result, List<ReactionDto>? Reactions)> SetOnMessageAsync(
        int groupId, int userId, int messageId, string? emoji);

    // voteId: the Vote row carrying the answer's FreeText (FreeTextResponseDto.VoteId).
    Task<(ReactionResult Result, List<ReactionDto>? Reactions)> SetOnAnswerAsync(
        int groupId, int userId, int voteId, string? emoji);
}
