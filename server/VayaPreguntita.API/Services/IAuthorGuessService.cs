using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Services;

// The guess (§17.6, §17.17) — author, chooser or flash launcher — and its hint (§17.10).
public interface IAuthorGuessService
{
    // The member's view of the guess on `entry` (Question loaded), or null when the question
    // isn't guessable. Call it only once the member has voted.
    Task<AuthorGuessDto?> GetStateAsync(DailyEntry entry, int userId);

    // The same for a flash ("¿Quién la lanzó?"), or null when its launcher isn't guessed.
    Task<AuthorGuessDto?> GetFlashStateAsync(FlashEntry flash, int userId, IReadOnlyCollection<int> memberIds);

    Task<(GuessResult result, GuessAuthorResponseDto? response)> GuessFlashAsync(
        int groupId,
        int userId,
        int flashId,
        GuessAuthorRequestDto dto
    );

    Task<(GuessResult result, AuthorHintResponseDto? response)> BuyFlashHintAsync(
        int groupId,
        int userId,
        int flashId,
        bool useOwned = false
    );

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
