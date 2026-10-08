using FluentAssertions;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.Enums;

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

    private static string Superlative(string key, string text) =>
        $$"""{ "key": "{{key}}", "type": "Superlative", "text": "{{text}}" }""";

    private static string Pack(string name, int order, params string[] templates) =>
        $$"""
            { "name": "{{name}}", "description": "d", "order": {{order}}, "disabledByDefault": false,
              "templates": [ {{string.Join(",", templates)}} ] }
            """;

    private static TemplateEntry Entry(string key, QuestionType type) => new(key, type, key);
}
