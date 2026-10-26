using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Tests.Testing;

// A TimeProvider pinned to an instant the test controls. Times are set as wall-clock
// times in the app's fixed UTC+2 zone (DailyClock), the way the daily time T is defined.
public sealed class TestClock : TimeProvider
{
    private DateTimeOffset _utcNow = new(2026, 10, 5, 10, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public DateTime UtcNow => _utcNow.UtcDateTime;

    public void SetLocal(DateOnly date, TimeOnly time) =>
        _utcNow = new DateTimeOffset(DailyClock.ToUtc(date, time), TimeSpan.Zero);

    public void Advance(TimeSpan by) => _utcNow += by;
}
