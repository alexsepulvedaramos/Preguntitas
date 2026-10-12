using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Daily;

// POST daily/vote: one vote per member on the open question, validated per type (§9),
// stored in the shape ResultsBuilder reads back.
[Collection(PostgresCollection.Name)]
public class VoteTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    // Opens a question of the given type today and parks the clock an hour after T.
    private async Task<(SeededGroup Group, int QuestionId, List<int> OptionIds)> OpenQuestionAsync(
        QuestionType type,
        Action<Question, SeededGroup>? configure = null
    )
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos", "dani");
        var questionId = await AddQuestionAsync(group.Id, type, configure: q => configure?.Invoke(q, group));
        await AddEntryAsync(group.Id, Today, questionId, group["ana"], activated: true);
        Clock.SetLocal(Today, DailyTime.AddHours(1));

        await using var db = NewContext();
        var optionIds = await db
            .Options.Where(o => o.QuestionId == questionId)
            .OrderBy(o => o.Id)
            .Select(o => o.Id)
            .ToListAsync();
        return (group, questionId, optionIds);
    }

    private async Task<(VoteResult Result, VoteResponseDto? Response)> VoteAsync(
        int groupId,
        int userId,
        CreateVoteDto dto
    )
    {
        await using var db = NewContext();
        return await NewDailyService(db).VoteAsync(groupId, userId, dto);
    }

    private async Task<List<Vote>> StoredVotesAsync()
    {
        await using var db = NewContext();
        return await db.Votes.OrderBy(v => v.Id).ToListAsync();
    }

    [Fact]
    public async Task AMemberVotesOnce_AndGetsTheFreshResults()
    {
        var (group, _, _) = await OpenQuestionAsync(QuestionType.Superlative);

        var (result, response) = await VoteAsync(group.Id, group["ana"], new() { SelectedTargetUserId = group["bea"] });
        var (again, _) = await VoteAsync(group.Id, group["ana"], new() { SelectedTargetUserId = group["carlos"] });

        result.Should().Be(VoteResult.Success);
        response!.Results.TotalVotes.Should().Be(1);
        response.Results.Results.Single().DisplayText.Should().Be("bea");
        again.Should().Be(VoteResult.AlreadyVoted);
        (await StoredVotesAsync()).Should().ContainSingle();
        Notifications.Verify(
            n => n.SendUserVotedAsync(group.Id, group["ana"], "Los de siempre", "ana"),
            Times.Once
        );
    }

    [Fact]
    public async Task WithOnlyAPendingQuestion_ThereIsNothingToVoteOn()
    {
        var group = await SeedGroupAsync("ana", "bea");
        var questionId = await AddQuestionAsync(group.Id);
        await AddEntryAsync(group.Id, Today, questionId, group["ana"], activated: false);
        Clock.SetLocal(Today, new TimeOnly(18, 0));

        var (result, _) = await VoteAsync(group.Id, group["ana"], new() { SelectedTargetUserId = group["bea"] });

        result.Should().Be(VoteResult.NoActiveQuestion);
    }

    [Fact]
    public async Task TheOpenQuestionCanStillBeVotedAfterMidnight()
    {
        var (group, questionId, _) = await OpenQuestionAsync(QuestionType.Superlative);
        Clock.SetLocal(Today.AddDays(1), new TimeOnly(0, 30));

        var (result, _) = await VoteAsync(group.Id, group["bea"], new() { SelectedTargetUserId = group["ana"] });

        result.Should().Be(VoteResult.Success);
        (await StoredVotesAsync()).Single().QuestionId.Should().Be(questionId);
    }

    [Fact]
    public async Task SecretPairing_StoresOneRowPerPredictedPartner()
    {
        var (group, _, _) = await OpenQuestionAsync(QuestionType.SecretPairing);

        var (result, response) = await VoteAsync(
            group.Id,
            group["ana"],
            new() { SelectedTargetUserIds = [group["carlos"], group["bea"]] }
        );

        result.Should().Be(VoteResult.Success);
        (await StoredVotesAsync())
            .Select(v => v.SelectedTargetUserId)
            .Should()
            .BeEquivalentTo(new int?[] { group["bea"], group["carlos"] });
        response!.Results.TotalVotes.Should().Be(1);
        response.Results.Results.Single().DisplayText.Should().Be("bea + carlos");
    }

    [Fact]
    public async Task Deathmatch_AcceptsTheTeamInAnyOrder_AndStoresItsLeader()
    {
        var (group, _, _) = await OpenQuestionAsync(
            QuestionType.Deathmatch,
            (q, g) => q.Metadata.Teams = [[g["ana"], g["bea"]], [g["carlos"], g["dani"]]]
        );

        var (result, response) = await VoteAsync(
            group.Id,
            group["bea"],
            new() { SelectedTargetUserIds = [group["dani"], group["carlos"]] }
        );

        result.Should().Be(VoteResult.Success);
        (await StoredVotesAsync()).Single().SelectedTargetUserId.Should().Be(group["carlos"]);
        response!.Results.Results.First().DisplayText.Should().Be("carlos + dani");
        response.Results.Results.First().VoteCount.Should().Be(1);
    }

    [Fact]
    public async Task Superlative_NobodyIsStoredWithoutATarget_WhenAllowed()
    {
        var (group, _, _) = await OpenQuestionAsync(QuestionType.Superlative, (q, _) => q.Metadata.AllowNobody = true);

        var (result, response) = await VoteAsync(group.Id, group["dani"], new() { SelectedTargetUserId = 0 });

        result.Should().Be(VoteResult.Success);
        (await StoredVotesAsync()).Single().SelectedTargetUserId.Should().BeNull();
        response!.Results.Results.Single().DisplayText.Should().Be("Nadie");
    }

    [Fact]
    public async Task CustomPoll_AnOtherAnswerCountsTowardsTheSelectionLimits()
    {
        var (group, _, optionIds) = await OpenQuestionAsync(
            QuestionType.CustomPoll,
            (q, _) =>
            {
                q.Metadata.MinSelections = 1;
                q.Metadata.MaxSelections = 2;
                q.Metadata.AllowOther = true;
                q.Options = [new Option { Text = "Pizza" }, new Option { Text = "Sushi" }];
            }
        );

        var (tooMany, _) = await VoteAsync(
            group.Id,
            group["ana"],
            new() { SelectedOptionIds = [optionIds[0], optionIds[1]], FreeText = "Croquetas" }
        );
        var (ok, response) = await VoteAsync(
            group.Id,
            group["ana"],
            new() { SelectedOptionIds = [optionIds[0]], FreeText = "  Croquetas  " }
        );

        tooMany.Should().Be(VoteResult.InvalidPayload);
        ok.Should().Be(VoteResult.Success);
        response!.Results.FreeTextResponses.Single().Text.Should().Be("Croquetas");
        (await StoredVotesAsync()).Should().HaveCount(2, "one row for the option and one for the free text");
    }

    // ── Invalid payloads ─────────────────────────────────────────────────────

    private sealed record InvalidVote(
        QuestionType Type,
        Action<Question, SeededGroup>? Configure,
        Func<SeededGroup, List<int>, CreateVoteDto> Vote
    );

    private const int Outsider = 999_999;

    private static readonly Dictionary<string, InvalidVote> InvalidVotes = new()
    {
        ["superlative: blacklisted member"] = new(
            QuestionType.Superlative,
            (q, g) => q.Metadata.BlacklistedUserIds = [g["bea"]],
            (g, _) => new() { SelectedTargetUserId = g["bea"] }
        ),
        ["superlative: not a member"] = new(
            QuestionType.Superlative,
            null,
            (_, _) => new() { SelectedTargetUserId = Outsider }
        ),
        ["superlative: nobody when not allowed"] = new(
            QuestionType.Superlative,
            null,
            (_, _) => new() { SelectedTargetUserId = 0 }
        ),
        ["superlative: no target"] = new(QuestionType.Superlative, null, (_, _) => new()),
        ["scale: below 1"] = new(
            QuestionType.Scale,
            (q, _) => (q.Metadata.RangeMin, q.Metadata.RangeMax) = (1, 10),
            (_, _) => new() { NumericValue = 0 }
        ),
        ["scale: above 10"] = new(
            QuestionType.Scale,
            (q, _) => (q.Metadata.RangeMin, q.Metadata.RangeMax) = (1, 10),
            (_, _) => new() { NumericValue = 11 }
        ),
        ["secret pairing: same person twice"] = new(
            QuestionType.SecretPairing,
            null,
            (g, _) => new() { SelectedTargetUserIds = [g["bea"], g["bea"]] }
        ),
        ["secret pairing: not a member"] = new(
            QuestionType.SecretPairing,
            null,
            (g, _) => new() { SelectedTargetUserIds = [g["bea"], Outsider] }
        ),
        ["deathmatch: not one of the teams"] = new(
            QuestionType.Deathmatch,
            (q, g) => q.Metadata.Teams = [[g["ana"], g["bea"]], [g["carlos"], g["dani"]]],
            (g, _) => new() { SelectedTargetUserIds = [g["ana"], g["carlos"]] }
        ),
        ["poll: more options than allowed"] = new(
            QuestionType.CustomPoll,
            (q, _) => q.Options = [new Option { Text = "Sí" }, new Option { Text = "No" }],
            (_, options) => new() { SelectedOptionIds = [options[0], options[1]] }
        ),
        ["poll: option from another question"] = new(
            QuestionType.CustomPoll,
            (q, _) => q.Options = [new Option { Text = "Sí" }, new Option { Text = "No" }],
            (_, _) => new() { SelectedOptionIds = [Outsider] }
        ),
        ["poll: free text when Otro is off"] = new(
            QuestionType.CustomPoll,
            (q, _) => q.Options = [new Option { Text = "Sí" }, new Option { Text = "No" }],
            (_, _) => new() { FreeText = "Depende" }
        ),
        ["open text: blank answer"] = new(QuestionType.OpenText, null, (_, _) => new() { FreeText = "   " }),
        ["open text: longer than 280"] = new(
            QuestionType.OpenText,
            null,
            (_, _) => new() { FreeText = new string('a', 281) }
        ),
    };

    public static TheoryData<string> InvalidVoteNames => new(InvalidVotes.Keys);

    [Theory]
    [MemberData(nameof(InvalidVoteNames))]
    public async Task AnInvalidVoteIsRejectedAndNothingIsStored(string name)
    {
        var invalid = InvalidVotes[name];
        var (group, _, optionIds) = await OpenQuestionAsync(invalid.Type, invalid.Configure);

        var (result, _) = await VoteAsync(group.Id, group["ana"], invalid.Vote(group, optionIds));

        result.Should().Be(VoteResult.InvalidPayload);
        (await StoredVotesAsync()).Should().BeEmpty();
    }
}
