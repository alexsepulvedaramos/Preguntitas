namespace VayaPreguntita.API.Localization;

// Languages the app is played in (rama 23). Spanish is the original and the fallback for
// anything unsupported — same rule as the client.
public static class AppLanguages
{
    public const string Spanish = "es";
    public const string English = "en";
    public const string German = "de";

    public const string Default = Spanish;

    public static readonly string[] Supported = [Spanish, English, German];

    public static bool IsSupported(string? lang) => lang is not null && Supported.Contains(lang);

    // Maps any tag ("en-US", "DE", null…) to a supported language code, else the default.
    public static string Normalize(string? lang)
    {
        var primary = lang?.Trim().Split('-', '_')[0].ToLowerInvariant();
        return IsSupported(primary) ? primary! : Default;
    }
}
