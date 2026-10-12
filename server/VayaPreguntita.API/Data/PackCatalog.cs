// Data/PackCatalog.cs
namespace VayaPreguntita.API.Data;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Localization;

// The question-pack catalog: one JSON file per pack under Data/Packs/, plus one translation
// file per pack and language under Data/Packs/{language}/ (rama 24), all embedded in the
// assembly. PackSeeder syncs it into the database at startup. Format and editing rules:
// Data/Packs/README.md.
public static partial class PackCatalog
{
    private const string ResourcePrefix = "Packs/";

    // PackTranslations/{language}/{file}.json — see the EmbeddedResource items in the csproj.
    private const string TranslationResourcePrefix = "PackTranslations/";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        // A misspelled property would otherwise be silently ignored (e.g. "retred": true).
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() },
    };

    public static IReadOnlyList<PackFile> Load() => Load(typeof(PackCatalog).Assembly);

    public static IReadOnlyList<PackFile> Load(Assembly assembly) =>
        Parse(
            ReadResources(assembly, ResourcePrefix),
            ReadResources(assembly, TranslationResourcePrefix)
                .Select(r => (language: r.name.Split('/')[1], r.name, r.json))
        );

    private static List<(string name, string json)> ReadResources(Assembly assembly, string prefix) =>
        assembly
            .GetManifestResourceNames()
            .Where(n => n.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal)
            .Select(name =>
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                using var reader = new StreamReader(stream);
                return (name, reader.ReadToEnd());
            })
            .ToList();

    // Parses and validates catalog files (and their translations, if any); throws
    // InvalidOperationException listing every problem.
    public static IReadOnlyList<PackFile> Parse(
        IEnumerable<(string name, string json)> files,
        IEnumerable<(string language, string name, string json)>? translationFiles = null
    )
    {
        var packs = files.Select(f => Deserialize<PackFile>(f.name, f.json)).ToList();
        var translations = (translationFiles ?? [])
            .Select(f => (f.language, f.name, file: Deserialize<PackTranslationFile>(f.name, f.json)))
            .ToList();

        var errors = new List<string>();
        packs = AttachTranslations(packs, translations, errors);
        errors.AddRange(Validate(packs));
        if (errors.Count > 0)
            throw new InvalidOperationException(
                "Pack catalog is invalid:" + Environment.NewLine + string.Join(Environment.NewLine, errors)
            );

        return [.. packs.OrderBy(p => p.Order)];
    }

    private static T Deserialize<T>(string name, string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? throw new JsonException("empty document");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Pack catalog file '{name}' is invalid: {ex.Message}", ex);
        }
    }

    // Hangs each translation file's texts on its pack and templates. Templates are matched by
    // key, and a translation must sit in the file of the template's own pack, so the files of
    // every language mirror the Spanish ones.
    private static List<PackFile> AttachTranslations(
        List<PackFile> packs,
        List<(string language, string name, PackTranslationFile file)> translations,
        List<string> errors
    )
    {
        var packByName = packs.GroupBy(p => p.Name).ToDictionary(g => g.Key, g => g.First());
        var packTranslations = packs.Distinct().ToDictionary(p => p, _ => new Dictionary<string, PackTranslation>());
        var templateTranslations =
            new Dictionary<(PackFile pack, string key), Dictionary<string, TemplateTranslation>>();

        foreach (var (language, name, file) in translations)
        {
            if (!ContentTranslations.Languages.Contains(language))
            {
                errors.Add($"{name}: '{language}' is not a translation language.");
                continue;
            }
            if (!packByName.TryGetValue(file.Pack ?? string.Empty, out var pack))
            {
                errors.Add($"{name}: pack '{file.Pack}' is not in the catalog.");
                continue;
            }
            if (!packTranslations[pack].TryAdd(language, new PackTranslation(file.Name, file.Description)))
            {
                errors.Add($"{name}: pack '{pack.Name}' already has a '{language}' translation file.");
                continue;
            }

            var keysInPack = pack.Templates.Select(t => t.Key).ToHashSet();
            foreach (var (key, translation) in file.Templates ?? [])
            {
                if (!keysInPack.Contains(key))
                {
                    var owner = packs.FirstOrDefault(p => p.Templates.Any(t => t.Key == key));
                    errors.Add(
                        owner is null
                            ? $"{name}: key '{key}' is not in the catalog."
                            : $"{name}: key '{key}' belongs to pack '{owner.Name}'; translate it in that pack's file."
                    );
                    continue;
                }

                if (!templateTranslations.TryGetValue((pack, key), out var byLanguage))
                    templateTranslations[(pack, key)] = byLanguage = [];
                byLanguage[language] = translation;
            }
        }

        return packs
            .Select(pack =>
                pack with
                {
                    Translations = packTranslations[pack],
                    Templates =
                    [
                        .. pack.Templates.Select(t =>
                            templateTranslations.TryGetValue((pack, t.Key), out var byLanguage)
                                ? t with { Translations = byLanguage }
                                : t
                        ),
                    ],
                }
            )
            .ToList();
    }

    public static List<string> Validate(IReadOnlyList<PackFile> packs)
    {
        var errors = new List<string>();
        var packNames = new HashSet<string>();
        var orders = new HashSet<int>();
        var keys = new HashSet<string>();
        var adoptionIdentities = new HashSet<(string, string)>();

        foreach (var pack in packs)
        {
            if (string.IsNullOrWhiteSpace(pack.Name) || !packNames.Add(pack.Name))
                errors.Add($"Pack name '{pack.Name}' is empty or duplicated.");
            if (!orders.Add(pack.Order))
                errors.Add($"Pack '{pack.Name}': order {pack.Order} is used by another pack.");
        }

        foreach (var pack in packs)
        {
            var textsInPack = new HashSet<string>();
            foreach (var t in pack.Templates)
            {
                var where = $"[{pack.Name}] {t.Key}";

                if (!KeyPattern().IsMatch(t.Key ?? string.Empty))
                    errors.Add($"{where}: key must be lowercase letters, digits and dashes.");
                else if (!keys.Add(t.Key))
                    errors.Add($"{where}: duplicated key.");

                if (string.IsNullOrWhiteSpace(t.Text))
                    errors.Add($"{where}: empty text.");
                else if (!textsInPack.Add(t.Text))
                    errors.Add($"{where}: duplicated text within the pack.");

                if (t.LegacyPack is not null && !packs.Any(p => p.Name == t.LegacyPack))
                    errors.Add($"{where}: legacyPack '{t.LegacyPack}' is not a pack in the catalog.");

                var identity = (t.LegacyPack ?? pack.Name, t.LegacyText ?? t.Text);
                if (!adoptionIdentities.Add(identity))
                    errors.Add($"{where}: two entries adopt the same legacy row {identity}.");

                ValidateShape(t, where, errors);
                ValidateTranslations(t, where, errors);
            }

            foreach (var (language, translation) in pack.Translations)
                if (string.IsNullOrWhiteSpace(translation.Name) || string.IsNullOrWhiteSpace(translation.Description))
                    errors.Add($"Pack '{pack.Name}' ({language}): empty name or description.");
        }

        return errors;
    }

    private static void ValidateShape(TemplateEntry t, string where, List<string> errors)
    {
        var options = t.Options ?? [];
        var metadata = t.ToMetadata();

        if (t.Type == QuestionType.CustomPoll)
        {
            if (options.Count < 2)
                errors.Add($"{where}: a poll needs at least 2 options.");
            if (options.Any(string.IsNullOrWhiteSpace) || options.Distinct().Count() != options.Count)
                errors.Add($"{where}: poll options must be non-empty and unique.");
            if (
                metadata.MinSelections < 1
                || metadata.MaxSelections < metadata.MinSelections
                || metadata.MaxSelections > options.Count
            )
                errors.Add($"{where}: invalid min/max selections.");
        }
        else if (options.Count > 0)
        {
            errors.Add($"{where}: only polls can define options.");
        }

        if (t.Type == QuestionType.SecretPairing && (metadata.MinSelections != 2 || metadata.MaxSelections != 2))
            errors.Add($"{where}: a secret pairing must require exactly 2 selections.");
    }

    // A translation is the same question in another language: every member of a group votes on
    // the same question whatever their language, so a poll keeps its options in the same order.
    private static void ValidateTranslations(TemplateEntry t, string where, List<string> errors)
    {
        var options = t.Options ?? [];
        foreach (var (language, translation) in t.Translations)
        {
            var at = $"{where} ({language})";
            var translatedOptions = translation.Options ?? [];

            if (string.IsNullOrWhiteSpace(translation.Text))
                errors.Add($"{at}: empty text.");

            if (t.Type != QuestionType.CustomPoll)
            {
                if (translatedOptions.Count > 0)
                    errors.Add($"{at}: only polls can define options.");
            }
            else if (translatedOptions.Count != options.Count)
            {
                errors.Add($"{at}: {translatedOptions.Count} options, but the Spanish poll has {options.Count}.");
            }
            else if (
                translatedOptions.Any(string.IsNullOrWhiteSpace)
                || translatedOptions.Distinct().Count() != translatedOptions.Count
            )
            {
                errors.Add($"{at}: poll options must be non-empty and unique.");
            }
        }
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex KeyPattern();
}

// One pack file. Template order inside the file is the order new rows are inserted in.
public sealed record PackFile(
    string Name,
    string Description,
    int Order,
    bool DisabledByDefault,
    List<TemplateEntry> Templates
)
{
    // From the translation files, keyed by language code.
    [JsonIgnore]
    public IReadOnlyDictionary<string, PackTranslation> Translations { get; init; } =
        new Dictionary<string, PackTranslation>();
}

public sealed record PackTranslation(string Name, string Description);

// One translation file (Data/Packs/{language}/{file}.json): the pack's display name and
// description, and its translated templates keyed by catalog key.
public sealed record PackTranslationFile(
    string Pack,
    string Name,
    string Description,
    Dictionary<string, TemplateTranslation> Templates
);

// Poll options in the same order as the Spanish ones.
public sealed record TemplateTranslation(string Text, List<string>? Options = null);

// One template of the catalog.
//
// Only metadata that differs from the per-type defaults is written in the file
// (see ToMetadata). `Retired` keeps the entry in the file but stops it being offered.
// `LegacyText` / `LegacyPack` are one-time adoption hints: they let the sync find a row that
// was seeded before the catalog existed (identified by pack + text) when the entry's wording
// or pack has since changed. They can be removed once every database has been synced.
public sealed record TemplateEntry(
    string Key,
    QuestionType Type,
    string Text,
    List<string>? Options = null,
    int? MinSelections = null,
    int? MaxSelections = null,
    bool? AllowOther = null,
    bool? AllowNobody = null,
    bool Retired = false,
    string? LegacyText = null,
    string? LegacyPack = null
)
{
    // From the translation files, keyed by language code.
    [JsonIgnore]
    public IReadOnlyDictionary<string, TemplateTranslation> Translations { get; init; } =
        new Dictionary<string, TemplateTranslation>();

    // Per-type defaults mirror what the old C# builders produced.
    public QuestionMetadata ToMetadata()
    {
        var selections = Type == QuestionType.SecretPairing ? 2 : 1;
        var metadata = new QuestionMetadata
        {
            MinSelections = MinSelections ?? selections,
            MaxSelections = MaxSelections ?? selections,
            AllowOther = AllowOther ?? false,
            AllowNobody = AllowNobody ?? false,
        };

        if (Type == QuestionType.Scale)
        {
            metadata.RangeMin = QuestionMetadata.ScaleMin;
            metadata.RangeMax = QuestionMetadata.ScaleMax;
        }

        return metadata;
    }
}
