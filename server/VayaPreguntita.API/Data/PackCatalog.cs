// Data/PackCatalog.cs
namespace VayaPreguntita.API.Data;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;

// The question-pack catalog: one JSON file per pack under Data/Packs/, embedded in the
// assembly. PackSeeder syncs it into the database at startup. Format and editing rules:
// Data/Packs/README.md.
public static partial class PackCatalog
{
    private const string ResourcePrefix = "Packs/";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        // A misspelled property would otherwise be silently ignored (e.g. "retred": true).
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter() },
    };

    public static IReadOnlyList<PackFile> Load() => Load(typeof(PackCatalog).Assembly);

    public static IReadOnlyList<PackFile> Load(Assembly assembly)
    {
        var files = assembly
            .GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal)
            .Select(name =>
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                using var reader = new StreamReader(stream);
                return (name, json: reader.ReadToEnd());
            });

        return Parse(files);
    }

    // Parses and validates catalog files; throws InvalidOperationException listing every problem.
    public static IReadOnlyList<PackFile> Parse(IEnumerable<(string name, string json)> files)
    {
        var packs = new List<PackFile>();
        foreach (var (name, json) in files)
        {
            try
            {
                packs.Add(
                    JsonSerializer.Deserialize<PackFile>(json, JsonOptions)
                        ?? throw new JsonException("empty document")
                );
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Pack catalog file '{name}' is invalid: {ex.Message}", ex);
            }
        }

        var errors = Validate(packs);
        if (errors.Count > 0)
            throw new InvalidOperationException(
                "Pack catalog is invalid:" + Environment.NewLine + string.Join(Environment.NewLine, errors)
            );

        return [.. packs.OrderBy(p => p.Order)];
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
            }
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

        if (t.Type == QuestionType.Scale && metadata.RangeMin >= metadata.RangeMax)
            errors.Add($"{where}: scale rangeMin must be lower than rangeMax.");
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
);

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
    int? RangeMin = null,
    int? RangeMax = null,
    bool Retired = false,
    string? LegacyText = null,
    string? LegacyPack = null
)
{
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
            metadata.RangeMin = RangeMin ?? 1;
            metadata.RangeMax = RangeMax ?? 10;
        }

        return metadata;
    }
}
