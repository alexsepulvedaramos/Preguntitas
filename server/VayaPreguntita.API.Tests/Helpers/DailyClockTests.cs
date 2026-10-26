using FluentAssertions;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Helpers;

// The daily time T is a local Madrid wall-clock time, so it keeps its local hour across
// the October/March clock changes (UTC+2 in summer, UTC+1 in winter).
public class DailyClockTests
{
    private static readonly TimeOnly DailyTime = new(21, 0);

    [Theory]
    [InlineData(2026, 10, 24, 19)] // summer time: 21:00 local = 19:00 UTC
    [InlineData(2026, 10, 26, 20)] // after the 25 Oct change: 21:00 local = 20:00 UTC
    [InlineData(2027, 3, 27, 20)]  // winter, day before spring-forward
    [InlineData(2027, 3, 29, 19)]  // summer again
    public void ToUtc_follows_the_daylight_saving_offset(int y, int m, int d, int utcHour) =>
        DailyClock.ToUtc(new DateOnly(y, m, d), DailyTime).Should().Be(new DateTime(y, m, d, utcHour, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Today_and_TimeOfDay_use_the_local_winter_offset()
    {
        var clock = new TestClock();
        clock.SetLocal(new DateOnly(2026, 10, 26), new TimeOnly(0, 30));

        DailyClock.Today(clock).Should().Be(new DateOnly(2026, 10, 26));
        DailyClock.TimeOfDay(clock).Should().Be(new TimeOnly(0, 30));
    }
}
