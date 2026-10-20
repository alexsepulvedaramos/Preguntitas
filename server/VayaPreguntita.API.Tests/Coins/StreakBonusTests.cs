using FluentAssertions;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Tests.Coins;

// The daily streak bonus (§17.15): continuing a streak must always pay at least as much as
// restarting it, for any length.
public class StreakBonusTests
{
    // Coins a single unbroken streak of `days` earns from the bonus.
    private static int Total(int days) => Enumerable.Range(1, days).Sum(CoinRewards.StreakBonus);

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 1)]
    [InlineData(6, 1)]
    [InlineData(7, 2)]
    [InlineData(15, 3)]
    [InlineData(30, 4)]
    [InlineData(99, 4)]
    [InlineData(100, 5)]
    [InlineData(1000, 5)]
    public void TheBonus_FollowsTheSteps(int streak, int expected) =>
        CoinRewards.StreakBonus(streak).Should().Be(expected);

    [Fact]
    public void TheBonus_NeverDecreasesAsTheStreakGrows()
    {
        var decreases = Enumerable
            .Range(1, 800)
            .Where(day => CoinRewards.StreakBonus(day + 1) < CoinRewards.StreakBonus(day));
        decreases.Should().BeEmpty();
    }

    // Splitting a run into two streaks costs the missed day in between; with this holding
    // for every pair, no split into any number of streaks beats the unbroken one.
    [Fact]
    public void OneUnbrokenStreak_BeatsAnySplit()
    {
        const int max = 800;
        var totals = Enumerable.Range(0, 2 * max + 2).Select(Total).ToArray();
        var splitsThatWin = new List<string>();
        for (var a = 1; a <= max; a++)
        for (var b = 1; b <= max; b++)
            if (totals[a + 1 + b] < totals[a] + totals[b])
                splitsThatWin.Add($"{a} + break + {b}");

        splitsThatWin.Should().BeEmpty();
    }
}
