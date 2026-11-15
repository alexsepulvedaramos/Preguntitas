using System.Text.RegularExpressions;

namespace VayaPreguntita.API.Helpers;

// The "seen" marks a client may store (rama 56) and how each one moves:
//   release, releaseAnnounced → an app version; only ever moves to a newer one.
//   hint:<name>               → "1" once the one-off hint is dismissed.
//   summary:<groupId>         → the newest month opened (YYYY-MM); only moves forward.
// Marks only move forward so a device that has been offline for weeks can't bring back
// what a newer one already dismissed.
public static partial class SeenMarks
{
    public const int MaxPerRequest = 100;
    public const int MaxPerUser = 200;
    public const int MaxKeyLength = 48;
    public const int MaxValueLength = 16;

    private const string Release = "release";
    private const string ReleaseAnnounced = "releaseAnnounced";
    private const string HintPrefix = "hint:";
    private const string SummaryPrefix = "summary:";

    [GeneratedRegex(@"^hint:[A-Za-z]{1,32}$")]
    private static partial Regex HintKey();

    [GeneratedRegex(@"^summary:[1-9]\d{0,9}$")]
    private static partial Regex SummaryKey();

    [GeneratedRegex(@"^\d{4}-(0[1-9]|1[0-2])$")]
    private static partial Regex Month();

    [GeneratedRegex(@"^\d{1,4}\.\d{1,4}\.\d{1,4}$")]
    private static partial Regex AppVersion();

    private static bool IsVersionKey(string key) => key is Release or ReleaseAnnounced;

    public static bool IsValid(string? key, string? value)
    {
        if (key is null || value is null)
            return false;
        if (IsVersionKey(key))
            return AppVersion().IsMatch(value);
        if (HintKey().IsMatch(key))
            return value == "1";
        if (SummaryKey().IsMatch(key))
            return Month().IsMatch(value);
        return false;
    }

    // Whether `incoming` (already valid) replaces what is stored.
    public static bool Advances(string key, string? stored, string incoming)
    {
        if (stored is null)
            return true;
        if (IsVersionKey(key))
            return !Version.TryParse(stored, out var old) || Version.Parse(incoming) > old;
        if (key.StartsWith(SummaryPrefix, StringComparison.Ordinal))
            return string.CompareOrdinal(incoming, stored) > 0;
        return false;
    }
}
