using System.Globalization;

namespace VayaPreguntita.API.Localization;

// Pack content in the player's language (rama 24). Spanish is the original and lives in the
// entity's own column (Text, Name…); the translation maps hold the other languages. A missing
// translation falls back to the Spanish original, so a half-translated catalog never breaks.
public static class ContentTranslations
{
    // Languages pack content is translated into. Spanish is not listed: it is the original.
    public static readonly string[] Languages = ["en", "de"];

    // The language of the current request: the UI language the client sends as
    // Accept-Language (CurrentUICulture). Anything unsupported reads the Spanish original.
    public static string RequestLanguage =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();

    public static string Pick(string original, IReadOnlyDictionary<string, string>? translations) =>
        Pick(original, translations, RequestLanguage);

    public static string Pick(
        string original,
        IReadOnlyDictionary<string, string>? translations,
        string language
    ) =>
        translations is not null
        && translations.TryGetValue(language, out var translated)
        && !string.IsNullOrWhiteSpace(translated)
            ? translated
            : original;
}
