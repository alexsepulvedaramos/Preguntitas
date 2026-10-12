using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Services;

// "Guess the author" (§17.6) and its hint (§17.10).
public interface IAuthorGuessService
{
    // The member's view of the guess on `entry` (Question loaded), or null when the question
    // isn't guessable. Call it only once the member has voted.
    Task<AuthorGuessDto?> GetStateAsync(DailyEntry entry, int userId);

    Task<(GuessResult result, GuessAuthorResponseDto? response)> GuessAsync(
        int groupId,
        int userId,
        GuessAuthorRequestDto dto
    );

    // Idempotent: buying it again returns the same candidates and charges nothing.
    // useOwned: takes a hint from the inventory instead of paying (rama 45).
    Task<(GuessResult result, AuthorHintResponseDto? response)> BuyHintAsync(
        int groupId,
        int userId,
        bool useOwned = false
    );
}
