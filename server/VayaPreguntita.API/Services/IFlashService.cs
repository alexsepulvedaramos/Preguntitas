using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Services;

// Flash questions (§17.11): bought for coins, launched at once, open 24 h next to the daily
// question. They never count for streaks, coins or monthly awards.
public interface IFlashService
{
    // Charges the price (or, with useOwned, takes one from the inventory, rama 45) and launches
    // it in one transaction: any failure refunds.
    Task<(FlashResult Result, LaunchFlashResponseDto? Response)> LaunchAsync(
        int groupId,
        int userId,
        CreateQuestionDto dto,
        bool useOwned = false
    );

    // Open flash → on-time vote, returns the fresh state. Closed flash → late vote from the
    // history (§17.7), returns null state. Neither earns anything nor pushes.
    Task<(FlashResult Result, FlashStateDto? State)> VoteAsync(
        int groupId,
        int userId,
        int flashId,
        CreateVoteDto dto
    );

    // The group's open flashes, oldest first (the `flashes` block of daily/current).
    Task<List<FlashStateDto>> GetOpenAsync(int groupId, int userId);

    // One flash as the current user sees it (after changing a vote, rama 43).
    Task<FlashStateDto> GetStateAsync(int flashId, int userId);

    // History detail, gated like a daily question (§17.7). null when it doesn't exist.
    Task<QuestionResultDto?> GetDetailAsync(int groupId, int userId, int flashId);
}
