namespace VayaPreguntita.API.Helpers;

// MVP time zone is fixed at UTC+2 (Spain/Madrid). All "today/tomorrow/T"
// comparisons in the daily lifecycle must go through here so the offset
// lives in one place. Per-group Group.TimeZoneId is Phase 2.
// The current instant always comes from an injected TimeProvider (TimeProvider.System
// in production) so the lifecycle can be tested at any fixed moment.
public static class DailyClock
{
    public static readonly TimeSpan UtcOffset = TimeSpan.FromHours(2);

    public static DateTime UtcNow(TimeProvider clock) => clock.GetUtcNow().UtcDateTime;

    public static DateTime Now(TimeProvider clock) => UtcNow(clock) + UtcOffset;

    public static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(Now(clock));

    public static TimeOnly TimeOfDay(TimeProvider clock) => TimeOnly.FromDateTime(Now(clock));

    // The daily time T of a given local (UTC+2) date, expressed as a UTC instant.
    // Used for closesAt / activatesAt: a question on local date D activates at D@time.
    public static DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time);
        return DateTime.SpecifyKind(local - UtcOffset, DateTimeKind.Utc);
    }
}
