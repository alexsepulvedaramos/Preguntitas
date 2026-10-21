using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Helpers;

// Coin amounts (§17.2). Proposals, tuned with the developer.
public static class CoinRewards
{
    public const int Vote = 5;
    public const int FirstVoter = 3;

    // Guess the author (§17.6) and its hint (§17.10).
    public const int GuessCorrect = 5;
    public const int AuthorHintPrice = 20;

    // Paid at activation when the selector made an active choice (§17.4: confirming the
    // auto-selected question counts), plus a bonus when they wrote it inline.
    public const int SelectorChoice = 5;
    public const int SelectorCreatedBonus = 5;

    // Race to select (§17.5): the member who chooses in the selector's place at T − 10 min.
    public const int RaceWon = 10;

    // Flash question (§17.11): launched at once, open 24 h. Cheap on purpose (§17.15): it
    // drives the group dynamic, so it should be used often.
    public const int FlashQuestionPrice = 100;

    // Change my vote and toque / nudge (§17.10, rama 43).
    public const int VoteChangePrice = 50;
    public const int NudgePrice = 10;

    // Max capped coins a user can earn per local day, across all groups (anti-farm).
    public const int DailyCap = 40;

    // Daily streak bonus (§17.15), paid with every on-time vote on top of CoinRewards.Vote:
    // (first streak day, coins per vote). It never decreases as the streak grows, so breaking
    // a streak can only drop you to a lower bonus and one unbroken streak always earns at
    // least as much as any split of the same days into shorter ones.
    public static readonly (int FromDay, int Coins)[] StreakBonusSteps =
    [
        (3, 1),
        (7, 2),
        (15, 3),
        (30, 4),
        (100, 5),
    ];

    public static int StreakBonus(int streak) =>
        StreakBonusSteps.LastOrDefault(s => streak >= s.FromDay).Coins;

    // One-off rewards for reaching a tier or a milestone, paid only the first time the user
    // ever reaches that streak (User.HighestStreakEver), so they can't be farmed by restarting.
    public static readonly IReadOnlyDictionary<int, int> StreakTiers = new Dictionary<int, int>
    {
        [3] = 5,
        [7] = 20,
        [15] = 35,
        [30] = 50,
        [100] = 100,
        [182] = 150,
        [365] = 300,
    };

    public static readonly IReadOnlyDictionary<int, int> StreakMilestones = new Dictionary<int, int>
    {
        [21] = 25,
        [50] = 50,
        [200] = 100,
        [300] = 150,
    };

    // Streak rewards (the daily bonus included, §17.15) and refunds skip the daily cap.
    public static bool IsCapped(CoinReason reason) =>
        reason
            is CoinReason.Vote
                or CoinReason.FirstVoter
                or CoinReason.SelectorChoice
                or CoinReason.SelectorCreated
                or CoinReason.GuessCorrect
                or CoinReason.RaceWon;
}
