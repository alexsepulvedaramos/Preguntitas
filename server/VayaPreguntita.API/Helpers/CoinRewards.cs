using VayaPreguntita.API.Enums;

namespace VayaPreguntita.API.Helpers;

// Coin amounts (§17.2). Proposals, tuned with the developer.
public static class CoinRewards
{
    public const int Vote = 5;
    public const int FirstVoter = 3;

    // Max capped coins a user can earn per local day, across all groups (anti-farm).
    public const int DailyCap = 40;

    // Paid every time a streak reaches the tier, so a long streak must always beat
    // rebuilding shorter ones: one 7-day streak (5 + 20) > two 3-day streaks (5 + 5).
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

    // Rewards for rare milestones skip the daily cap.
    public static bool IsCapped(CoinReason reason) =>
        reason is CoinReason.Vote or CoinReason.FirstVoter;
}
