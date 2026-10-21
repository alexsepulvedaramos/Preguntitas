namespace VayaPreguntita.API.Enums;

// Why a coin transaction happened (§17.2). Stored as int — never renumber.
public enum CoinReason
{
    Vote = 0,
    FirstVoter = 1,
    StreakTier = 2,
    StreakMilestone = 3,
    SelectorChoice = 4,
    SelectorCreated = 5,

    // Spending (rama 35)
    StreakFreeze = 6,
    StreakFreezeGift = 7,
    StreakRepair = 8,

    // Guess the author (rama 36)
    GuessCorrect = 9,
    AuthorHint = 10,

    // Race to select (rama 40)
    RaceWon = 11,

    // Flash question (rama 44)
    FlashQuestion = 12,

    // Shop cosmetics (rama 42); RefId = the ShopCatalog item id
    Cosmetic = 13,

    // Shop and economy revision (rama 46, §17.15)
    StreakBonus = 14,

    // A retired cosmetic given back at its price; RefId = the ShopCatalog item id
    CosmeticRefund = 15,

    // Change my vote (rama 43); RefId = the question
    VoteChange = 16,

    // Toque / nudge (rama 43); RefId = the Nudge row
    Nudge = 17,

    // Flash variants (rama 50); RefId = the flash
    FlashAnonymous = 18,
    FlashCapsule = 19,
    FlashExtension = 20,
}
