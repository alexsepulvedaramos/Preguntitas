namespace VayaPreguntita.API.Helpers;

// MVP time zone is fixed at UTC+2 (Spain/Madrid). All "today/tomorrow/T"
// comparisons in the daily lifecycle must go through here so the offset
// lives in one place. Per-group Group.TimeZoneId is Phase 2.
public static class DailyClock
{
    public static readonly TimeSpan UtcOffset = TimeSpan.FromHours(2);

    public static DateTime Now() => DateTime.UtcNow + UtcOffset;

    public static DateOnly Today() => DateOnly.FromDateTime(Now());

    public static TimeOnly TimeOfDay() => TimeOnly.FromDateTime(Now());
}
