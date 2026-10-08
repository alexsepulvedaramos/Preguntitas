using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Tests.Packs;

// Catalog → database sync, against a real PostgreSQL (Testcontainers).
public class PackSeederTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await using var db = NewContext();
        db.QuestionTemplates.RemoveRange(await db.QuestionTemplates.ToListAsync());
        db.Packs.RemoveRange(await db.Packs.ToListAsync());
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Sync_OnEmptyDatabase_CreatesPacksAndTemplates()
    {
        var catalog = new[]
        {
            Pack("P", 1, Entry("p-1", "first"), Poll("p-2", "second", ["a", "b"])),
            Pack("Q", 2, [Entry("q-1", "third")], disabledByDefault: true),
        };

        var summary = await SyncAsync(catalog);

        summary.Should().Be(new PackSeeder.SyncSummary(Created: 3, Adopted: 0, Updated: 0));
        await using var db = NewContext();
        (await db.Packs.CountAsync()).Should().Be(2);
        (await db.Packs.SingleAsync(p => p.Name == "Q")).DisabledByDefault.Should().BeTrue();
        var poll = await db
            .QuestionTemplates.Include(t => t.Options)
            .SingleAsync(t => t.Key == "p-2");
        poll.Options.Select(o => o.Text).Should().Equal("a", "b");
        poll.IsRetired.Should().BeFalse();
    }

    [Fact]
    public async Task Sync_IsIdempotent()
    {
        var catalog = new[]
        {
            Pack("P", 1, Entry("p-1", "first"), Poll("p-2", "second", ["a", "b"])),
        };

        await SyncAsync(catalog);
        var second = await SyncAsync(catalog);

        second.Should().Be(new PackSeeder.SyncSummary(0, 0, 0));
        await using var db = NewContext();
        (await db.QuestionTemplates.CountAsync()).Should().Be(2);
        (await db.QuestionTemplates.SumAsync(t => t.Options.Count)).Should().Be(2);
    }

    [Fact]
    public async Task Sync_AdoptsLegacyRows_PreservingIdsAcrossRewriteAndMove()
    {
        var oldPack = await InsertPackAsync("Old pack");
        var pack = await InsertPackAsync("P");
        var rewritten = await InsertLegacyAsync(pack, "old wording");
        var unchanged = await InsertLegacyAsync(pack, "unchanged");
        var moved = await InsertLegacyAsync(oldPack, "moved");

        var catalog = new[]
        {
            Pack(
                "P",
                1,
                Entry("k-rewritten", "new wording") with
                {
                    LegacyText = "old wording",
                },
                Entry("k-unchanged", "unchanged")
            ),
            Pack("Q", 2, Entry("k-moved", "moved") with { LegacyPack = "Old pack" }),
            Pack("Old pack", 3, Entry("k-fresh", "fresh")),
        };

        var summary = await SyncAsync(catalog);

        summary.Created.Should().Be(1);
        summary.Adopted.Should().Be(3);
        await using var db = NewContext();
        (await db.QuestionTemplates.CountAsync())
            .Should()
            .Be(4, because: "adoption must not duplicate rows");

        var rewrittenRow = await db.QuestionTemplates.SingleAsync(t => t.Id == rewritten);
        rewrittenRow.Key.Should().Be("k-rewritten");
        rewrittenRow.Text.Should().Be("new wording");

        (await db.QuestionTemplates.SingleAsync(t => t.Id == unchanged))
            .Key.Should()
            .Be("k-unchanged");

        var movedRow = await db
            .QuestionTemplates.Include(t => t.Pack)
            .SingleAsync(t => t.Id == moved);
        movedRow.Key.Should().Be("k-moved");
        movedRow.Pack.Name.Should().Be("Q");
    }

    [Fact]
    public async Task Sync_RetiresAndUnretiresWithoutDeletingRows()
    {
        await SyncAsync([Pack("P", 1, Entry("k", "text") with { Retired = true })]);

        await using (var db = NewContext())
        {
            var row = await db.QuestionTemplates.SingleAsync();
            row.IsRetired.Should().BeTrue();
        }

        await SyncAsync([Pack("P", 1, Entry("k", "text"))]);

        await using (var db = NewContext())
        {
            (await db.QuestionTemplates.SingleAsync()).IsRetired.Should().BeFalse();
        }
    }

    [Fact]
    public async Task Sync_ReplacesOptionsAndMetadataWhenTheCatalogChanges()
    {
        await SyncAsync([Pack("P", 1, Poll("k", "poll", ["a", "b"]))]);

        await SyncAsync([
            Pack(
                "P",
                1,
                Poll("k", "poll", ["a", "b", "c"]) with
                {
                    MaxSelections = 2,
                    AllowOther = true,
                }
            ),
        ]);

        await using var db = NewContext();
        var row = await db.QuestionTemplates.Include(t => t.Options).SingleAsync();
        row.Options.OrderBy(o => o.Id).Select(o => o.Text).Should().Equal("a", "b", "c");
        row.Metadata.MaxSelections.Should().Be(2);
        row.Metadata.AllowOther.Should().BeTrue();
    }

    [Fact]
    public async Task Sync_NeverDeletesRowsMissingFromTheCatalog()
    {
        await SyncAsync([
            Pack("P", 1, Entry("gone", "will leave the catalog"), Entry("stays", "stays")),
        ]);

        await SyncAsync([Pack("P", 1, Entry("stays", "stays"))]);

        await using var db = NewContext();
        (await db.QuestionTemplates.CountAsync(t => t.Key == "gone")).Should().Be(1);
    }

    [Fact]
    public async Task Sync_RefusesToChangeATemplateType()
    {
        await SyncAsync([Pack("P", 1, Entry("k", "text"))]);

        var act = () =>
            SyncAsync([Pack("P", 1, Entry("k", "text") with { Type = QuestionType.Scale })]);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot change in place*");
    }

    // The production scenario: rows seeded by the old C# seeder (no Key), then the first startup
    // with the catalog. Every entry's legacy identity (pack + text, or its legacyPack/legacyText
    // hints) must resolve to exactly one row — none duplicated, none left behind. The simulation
    // seeds a legacy row for every entry, including ones added after the old seeder existed.
    [Fact]
    public async Task Sync_AdoptsEveryLegacyRowOfTheEmbeddedCatalog()
    {
        var catalog = PackCatalog.Load();
        await using (var db = NewContext())
        {
            var packs = catalog.ToDictionary(
                p => p.Name,
                p => new Pack
                {
                    Name = p.Name,
                    Description = "legacy",
                    IsActiveByDefault = true,
                }
            );
            db.Packs.AddRange(packs.Values);

            foreach (var file in catalog)
            foreach (var entry in file.Templates)
                packs[entry.LegacyPack ?? file.Name]
                    .Templates.Add(
                        new QuestionTemplate
                        {
                            Text = entry.LegacyText ?? entry.Text,
                            Type = entry.Type,
                            Metadata = entry.ToMetadata(),
                            Options =
                            [
                                .. (entry.Options ?? []).Select(o => new QuestionTemplateOption
                                {
                                    Text = o,
                                }),
                            ],
                        }
                    );
            await db.SaveChangesAsync();
        }

        var total = catalog.Sum(p => p.Templates.Count);
        var first = await SyncAsync(catalog);
        var second = await SyncAsync(catalog);

        first.Created.Should().Be(0);
        first.Adopted.Should().Be(total);
        second.Should().Be(new PackSeeder.SyncSummary(0, 0, 0));

        await using var verify = NewContext();
        (await verify.QuestionTemplates.CountAsync()).Should().Be(total);
        (await verify.QuestionTemplates.CountAsync(t => t.Key == null)).Should().Be(0);
        (await verify.QuestionTemplates.CountAsync(t => t.IsRetired))
            .Should()
            .Be(catalog.Sum(p => p.Templates.Count(t => t.Retired)));
    }

    [Fact]
    public async Task Sync_FromScratchWithTheEmbeddedCatalog_IsStableOnTheSecondRun()
    {
        var catalog = PackCatalog.Load();
        var total = catalog.Sum(p => p.Templates.Count);

        var first = await SyncAsync(catalog);
        var second = await SyncAsync(catalog);

        first.Created.Should().Be(total);
        second.Should().Be(new PackSeeder.SyncSummary(0, 0, 0));
    }

    [Fact]
    public async Task ReusePolicy_TreatsRetiredTemplatesAsUnavailable()
    {
        await SyncAsync([
            Pack("P", 1, Entry("retired", "retired") with { Retired = true }),
            Pack("Q", 2, Entry("active", "active")),
        ]);
        await using var db = NewContext();
        var onlyRetiredPack = (await db.Packs.SingleAsync(p => p.Name == "P")).Id;
        var activePack = (await db.Packs.SingleAsync(p => p.Name == "Q")).Id;
        const int groupWithoutHistory = 987_654;

        var exhaustedWithOnlyRetired = await TemplateReusePolicy.IsGroupExhaustedAsync(
            db,
            groupWithoutHistory,
            [onlyRetiredPack],
            []
        );
        var exhaustedWithActive = await TemplateReusePolicy.IsGroupExhaustedAsync(
            db,
            groupWithoutHistory,
            [onlyRetiredPack, activePack],
            []
        );

        exhaustedWithOnlyRetired.Should().BeTrue();
        exhaustedWithActive.Should().BeFalse();
    }

    // ---- helpers ----

    private AppDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(fixture.ConnectionString).Options
        );

    private async Task<PackSeeder.SyncSummary> SyncAsync(IReadOnlyList<PackFile> catalog)
    {
        await using var db = NewContext();
        return await PackSeeder.SyncCatalogAsync(db, catalog);
    }

    private async Task<int> InsertPackAsync(string name)
    {
        await using var db = NewContext();
        var pack = new Pack
        {
            Name = name,
            Description = "legacy",
            IsActiveByDefault = true,
        };
        db.Packs.Add(pack);
        await db.SaveChangesAsync();
        return pack.Id;
    }

    // A row as the old C# seeder left it: no Key.
    private async Task<int> InsertLegacyAsync(int packId, string text)
    {
        await using var db = NewContext();
        var template = new QuestionTemplate
        {
            Text = text,
            Type = QuestionType.Superlative,
            PackId = packId,
        };
        db.QuestionTemplates.Add(template);
        await db.SaveChangesAsync();
        return template.Id;
    }

    private static PackFile Pack(string name, int order, params TemplateEntry[] templates) =>
        new(name, "description", order, DisabledByDefault: false, [.. templates]);

    private static PackFile Pack(
        string name,
        int order,
        TemplateEntry[] templates,
        bool disabledByDefault
    ) => new(name, "description", order, disabledByDefault, [.. templates]);

    private static TemplateEntry Entry(string key, string text) =>
        new(key, QuestionType.Superlative, text);

    private static TemplateEntry Poll(string key, string text, string[] options) =>
        new(key, QuestionType.CustomPoll, text, [.. options]);
}
