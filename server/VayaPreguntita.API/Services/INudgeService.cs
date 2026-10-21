using VayaPreguntita.API.DTOs.Nudges;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Services;

// Toque / nudge (rama 43, §17.10): a push reminding a member to vote an open question.
public interface INudgeService
{
    // Members who haven't voted the question (you excluded) and whether a nudge reaches them.
    // flashId null = today's question. null when that question isn't open.
    Task<List<NudgeTargetDto>?> GetTargetsAsync(int groupId, int userId, int? flashId);

    Task<(NudgeResult Result, SendNudgeResponseDto? Response)> SendAsync(
        int groupId,
        int userId,
        SendNudgeRequestDto dto
    );
}
