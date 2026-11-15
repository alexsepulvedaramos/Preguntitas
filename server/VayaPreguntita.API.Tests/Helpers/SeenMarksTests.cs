using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Services;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Helpers;

// "Seen" marks kept per account (rama 56): the release notes opened, the one-off hints
// dismissed and the last monthly summary opened per group follow the player across devices.
public class SeenMarksRulesTests
{
    [Theory]
    [InlineData("release", "1.34.1", true)]
    [InlineData("releaseAnnounced", "1.34.0", true)]
    [InlineData("release", "latest", false)]
    [InlineData("hint:flashButton", "1", true)]
    [InlineData("hint:flashButton", "yes", false)]
    [InlineData("hint:", "1", false)]
    [InlineData("summary:12", "2026-10", true)]
    [InlineData("summary:12", "2026-13", false)]
    [InlineData("summary:0", "2026-10", false)]
    [InlineData("theme", "dark", false)]
    public void OnlyKnownMarksAreAccepted(string key, string value, bool expected) =>
        SeenMarks.IsValid(key, value).Should().Be(expected);

    [Theory]
    [InlineData("release", null, "1.2.0", true)]
    [InlineData("release", "1.9.0", "1.10.0", true)]
    [InlineData("release", "1.10.0", "1.9.0", false)]
    [InlineData("summary:3", "2026-09", "2026-10", true)]
    [InlineData("summary:3", "2026-10", "2026-09", false)]
    [InlineData("hint:changeVote", "1", "1", false)]
    public void AMarkOnlyMovesForward(string key, string? stored, string incoming, bool expected) =>
        SeenMarks.Advances(key, stored, incoming).Should().Be(expected);
}

[Collection(PostgresCollection.Name)]
public class SeenMarksServiceTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private async Task<Dictionary<string, string>?> SaveAsync(
        int userId,
        Dictionary<string, string> marks
    )
    {
        await using var db = NewContext();
        return await new SeenMarksService(db).SaveAsync(userId, marks);
    }

    [Fact]
    public async Task MarksAreKeptPerUser_AndMergedAcrossDevices()
    {
        var group = await SeedGroupAsync("ana", "bea");

        await SaveAsync(group["ana"], new() { ["release"] = "1.34.1", ["hint:flashButton"] = "1" });
        // An older device uploads what it had: nothing goes back, the new key is added.
        var merged = await SaveAsync(
            group["ana"],
            new() { ["release"] = "1.30.0", ["summary:1"] = "2026-10" }
        );

        merged
            .Should()
            .BeEquivalentTo(
                new Dictionary<string, string>
                {
                    ["release"] = "1.34.1",
                    ["hint:flashButton"] = "1",
                    ["summary:1"] = "2026-10",
                }
            );

        await using var db = NewContext();
        (await new SeenMarksService(db).GetAsync(group["bea"])).Should().BeEmpty();
    }

    [Fact]
    public async Task AnUnknownMark_StoresNothing()
    {
        var group = await SeedGroupAsync("ana", "bea");

        (await SaveAsync(group["ana"], new() { ["release"] = "1.34.1", ["theme"] = "dark" }))
            .Should()
            .BeNull();

        await using var db = NewContext();
        (await db.UserSeenMarks.CountAsync()).Should().Be(0);
    }
}
