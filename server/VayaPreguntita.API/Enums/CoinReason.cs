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
}
