namespace VayaPreguntita.API.Helpers;

// The daily lifecycle runs on Spain/Madrid wall-clock time, DST included (UTC+1 in winter,
// UTC+2 in summer). A group's daily time T is a local wall-clock time, so it must keep
// meaning the same local hour across the clock changes. All "today/tomorrow/T" comparisons
// go through here so the zone lives in one place. Per-group Group.TimeZoneId is Phase 2.
// The current instant always comes from an injected TimeProvider (TimeProvider.System
// in production) so the lifecycle can be tested at any fixed moment.
public static class DailyClock
{
    private static readonly TimeZoneInfo Zone = FindZone();

    private static TimeZoneInfo FindZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"); }
    }

    public static DateTime UtcNow(TimeProvider clock) => clock.GetUtcNow().UtcDateTime;

    public static DateTime Now(TimeProvider clock) => TimeZoneInfo.ConvertTimeFromUtc(UtcNow(clock), Zone);

    public static DateOnly Today(TimeProvider clock) => DateOnly.FromDateTime(Now(clock));

    public static TimeOnly TimeOfDay(TimeProvider clock) => TimeOnly.FromDateTime(Now(clock));

    // The daily time T of a given local date, expressed as a UTC instant. Used for
    // closesAt / activatesAt: a question on local date D activates at D@time.
    public static DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        // A wall-clock time skipped by the spring-forward gap resolves to the first valid
        // instant after it; an ambiguous one (autumn fall-back) resolves to standard time (the second occurrence).
        if (Zone.IsInvalidTime(local))
            local = local.AddHours(1);
        return TimeZoneInfo.ConvertTimeToUtc(local, Zone);
    }
}
