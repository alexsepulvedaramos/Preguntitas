using FluentAssertions;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Localization;

namespace VayaPreguntita.API.Tests.Packs;

// Pure tests over the embedded catalog (Data/Packs/*.json) — no database needed.
public class PackCatalogTests
{
    [Fact]
    public void EmbeddedCatalog_LoadsAndIsValid()
    {
        var packs = PackCatalog.Load();

        packs.Should().HaveCount(18);
        PackCatalog.Validate(packs).Should().BeEmpty();
    }

    [Fact]
    public void EmbeddedCatalog_KeepsHumorNegroAndGuarradasOffByDefault()
    {
        var packs = PackCatalog.Load().ToDictionary(p => p.Name);

        packs["Humor negro"].DisabledByDefault.Should().BeTrue();
        packs["Guarradas y dilemas asquerosos"].DisabledByDefault.Should().BeTrue();
        packs["Base"].DisabledByDefault.Should().BeFalse();
    }

    [Fact]
    public void EmbeddedCatalog_EveryPackStillOffersActiveTemplates()
    {
        // The §6.1 invariant (there is always a question) relies on the Base pack in particular.
        foreach (var pack in PackCatalog.Load())
            pack.Templates.Count(t => !t.Retired)
                .Should()
                .BeGreaterThan(0, because: $"pack '{pack.Name}' must keep active templates");
    }

    [Fact]
    public void EmbeddedCatalog_TranslatesEveryActiveTemplateIntoEveryLanguage()
    {
        // Every player reads pack questions in their own language (rama 24): a gap would show
        // them the Spanish original.
        foreach (var pack in PackCatalog.Load())
        foreach (var language in ContentTranslations.Languages)
        {
            pack.Translations.Should().ContainKey(language, because: $"pack '{pack.Name}' needs a {language} name");
            pack.Templates.Where(t => !t.Retired)
                .Where(t => !t.Translations.ContainsKey(language))
                .Select(t => t.Key)
                .Should()
                .BeEmpty(because: $"every active template of '{pack.Name}' needs a {language} translation");
        }
    }

    [Fact]
    public void ToMetadata_AppliesPerTypeDefaults()
    {
        var pairing = Entry("a", QuestionType.SecretPairing).ToMetadata();
        pairing.MinSelections.Should().Be(2);
        pairing.MaxSelections.Should().Be(2);

        var scale = Entry("b", QuestionType.Scale).ToMetadata();
        scale.RangeMin.Should().Be(1);
        scale.RangeMax.Should().Be(10);

        var superlative = Entry("c", QuestionType.Superlative).ToMetadata();
        superlative.RangeMin.Should().BeNull();
        superlative.MinSelections.Should().Be(1);
        superlative.AllowNobody.Should().BeFalse();
    }

    [Fact]
    public void Parse_RejectsDuplicateKeys()
    {
        var act = () =>
            PackCatalog.Parse([
                ("p.json", Pack("P", 1, Superlative("k", "one"), Superlative("k", "two"))),
            ]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*duplicated key*");
    }

    [Fact]
    public void Parse_RejectsDuplicateTextInSamePack()
    {
        var act = () =>
            PackCatalog.Parse([
                ("p.json", Pack("P", 1, Superlative("k1", "same"), Superlative("k2", "same"))),
            ]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*duplicated text*");
    }

    [Fact]
    public void Parse_RejectsPollWithSingleOption()
    {
        var poll = """{ "key": "k", "type": "CustomPoll", "text": "t", "options": ["only"] }""";

        var act = () => PackCatalog.Parse([("p.json", Pack("P", 1, poll))]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*at least 2 options*");
    }

    [Fact]
    public void Parse_RejectsOptionsOnNonPoll()
    {
        var bad = """{ "key": "k", "type": "Superlative", "text": "t", "options": ["a", "b"] }""";

        var act = () => PackCatalog.Parse([("p.json", Pack("P", 1, bad))]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*only polls*");
    }

    [Fact]
    public void Parse_RejectsMaxSelectionsAboveOptionCount()
    {
        var bad =
            """{ "key": "k", "type": "CustomPoll", "text": "t", "options": ["a", "b"], "maxSelections": 3 }""";

        var act = () => PackCatalog.Parse([("p.json", Pack("P", 1, bad))]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*min/max selections*");
    }

    [Fact]
    public void Parse_RejectsMisspelledProperty()
    {
        var bad = """{ "key": "k", "type": "Superlative", "text": "t", "retred": true }""";

        var act = () => PackCatalog.Parse([("p.json", Pack("P", 1, bad))]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*retred*");
    }

    [Fact]
    public void Parse_RejectsUnknownLegacyPack()
    {
        var bad = """{ "key": "k", "type": "Superlative", "text": "t", "legacyPack": "Nope" }""";

        var act = () => PackCatalog.Parse([("p.json", Pack("P", 1, bad))]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*legacyPack*");
    }

    [Fact]
    public void Parse_RejectsTwoEntriesAdoptingTheSameLegacyRow()
    {
        var a = """{ "key": "a", "type": "Superlative", "text": "new a", "legacyText": "old" }""";
        var b = """{ "key": "b", "type": "Superlative", "text": "new b", "legacyText": "old" }""";

        var act = () => PackCatalog.Parse([("p.json", Pack("P", 1, a, b))]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*adopt the same legacy row*");
    }

    [Fact]
    public void Parse_OrdersPacksByOrderField()
    {
        var packs = PackCatalog.Parse([
            ("b.json", Pack("Second", 2, Superlative("b1", "b"))),
            ("a.json", Pack("First", 1, Superlative("a1", "a"))),
        ]);

        packs.Select(p => p.Name).Should().Equal("First", "Second");
    }

    // ---- translations (rama 24) ----

    [Fact]
    public void Parse_AttachesTranslationsToTheirPackAndTemplates()
    {
        var poll = """{ "key": "poll", "type": "CustomPoll", "text": "¿Playa o montaña?", "options": ["Playa", "Montaña"] }""";
        var en = Translation("P", """
            "who": { "text": "Who would be late?" },
            "poll": { "text": "Beach or mountains?", "options": ["Beach", "Mountains"] }
            """);

        var pack = PackCatalog
            .Parse([("p.json", Pack("P", 1, Superlative("who", "¿Quién llegaría tarde?"), poll))], [("en", "en/p.json", en)])
            .Single();

        pack.Translations["en"].Should().Be(new PackTranslation("P in English", "d in English"));
        var byKey = pack.Templates.ToDictionary(t => t.Key);
        byKey["who"].Translations["en"].Text.Should().Be("Who would be late?");
        byKey["poll"].Translations["en"].Options.Should().Equal("Beach", "Mountains");
        byKey["who"].Translations.Should().NotContainKey("de");
    }

    [Fact]
    public void Parse_LeavesUntranslatedTemplatesWithoutTranslations()
    {
        var pack = PackCatalog
            .Parse([("p.json", Pack("P", 1, Superlative("a", "uno"), Superlative("b", "dos")))], [
                ("de", "de/p.json", Translation("P", """ "a": { "text": "eins" } """)),
            ])
            .Single();

        pack.Templates.Single(t => t.Key == "b").Translations.Should().BeEmpty();
    }

    [Fact]
    public void Parse_RejectsATranslationOfAnUnknownKey()
    {
        var act = () =>
            PackCatalog.Parse([("p.json", Pack("P", 1, Superlative("a", "uno")))], [
                ("en", "en/p.json", Translation("P", """ "nope": { "text": "one" } """)),
            ]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*'nope' is not in the catalog*");
    }

    [Fact]
    public void Parse_RejectsATranslationInAnotherPacksFile()
    {
        var act = () =>
            PackCatalog.Parse(
                [("p.json", Pack("P", 1, Superlative("a", "uno"))), ("q.json", Pack("Q", 2, Superlative("b", "dos")))],
                [("en", "en/q.json", Translation("Q", """ "a": { "text": "one" } """))]
            );

        act.Should().Throw<InvalidOperationException>().WithMessage("*belongs to pack 'P'*");
    }

    [Fact]
    public void Parse_RejectsATranslatedPollWithADifferentNumberOfOptions()
    {
        var poll = """{ "key": "poll", "type": "CustomPoll", "text": "t", "options": ["a", "b", "c"] }""";

        var act = () =>
            PackCatalog.Parse([("p.json", Pack("P", 1, poll))], [
                ("en", "en/p.json", Translation("P", """ "poll": { "text": "t", "options": ["a", "b"] } """)),
            ]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*2 options, but the Spanish poll has 3*");
    }

    [Fact]
    public void Parse_RejectsOptionsOnATranslatedNonPoll()
    {
        var act = () =>
            PackCatalog.Parse([("p.json", Pack("P", 1, Superlative("a", "uno")))], [
                ("en", "en/p.json", Translation("P", """ "a": { "text": "one", "options": ["x", "y"] } """)),
            ]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*(en): only polls*");
    }

    [Fact]
    public void Parse_RejectsAnEmptyTranslatedText()
    {
        var act = () =>
            PackCatalog.Parse([("p.json", Pack("P", 1, Superlative("a", "uno")))], [
                ("en", "en/p.json", Translation("P", """ "a": { "text": " " } """)),
            ]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*(en): empty text*");
    }

    [Fact]
    public void Parse_RejectsAnUnsupportedLanguage()
    {
        var act = () =>
            PackCatalog.Parse([("p.json", Pack("P", 1, Superlative("a", "uno")))], [
                ("fr", "fr/p.json", Translation("P", """ "a": { "text": "un" } """)),
            ]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*'fr' is not a translation language*");
    }

    [Fact]
    public void Parse_RejectsTwoTranslationFilesForTheSamePackAndLanguage()
    {
        var act = () =>
            PackCatalog.Parse([("p.json", Pack("P", 1, Superlative("a", "uno")))], [
                ("en", "en/p.json", Translation("P", """ "a": { "text": "one" } """)),
                ("en", "en/p-again.json", Translation("P", """ "a": { "text": "one" } """)),
            ]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*already has a 'en' translation file*");
    }

    [Fact]
    public void Parse_RejectsAMisspelledPropertyInATranslation()
    {
        var act = () =>
            PackCatalog.Parse([("p.json", Pack("P", 1, Superlative("a", "uno")))], [
                ("en", "en/p.json", Translation("P", """ "a": { "txt": "one" } """)),
            ]);

        act.Should().Throw<InvalidOperationException>().WithMessage("*txt*");
    }

    private static string Translation(string pack, string templates) =>
        $$"""
            { "pack": "{{pack}}", "name": "{{pack}} in English", "description": "d in English",
              "templates": { {{templates}} } }
            """;

    private static string Superlative(string key, string text) =>
        $$"""{ "key": "{{key}}", "type": "Superlative", "text": "{{text}}" }""";

    private static string Pack(string name, int order, params string[] templates) =>
        $$"""
            { "name": "{{name}}", "description": "d", "order": {{order}}, "disabledByDefault": false,
              "templates": [ {{string.Join(",", templates)}} ] }
            """;

    private static TemplateEntry Entry(string key, QuestionType type) => new(key, type, key);
}
