using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Localization;
using VayaPreguntita.API.Services;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Questions;

// POST questions: the pool quotas (§11 — 20 unused per member per group, 500 per group)
// and the membership rules a new question must satisfy (§9).
[Collection(PostgresCollection.Name)]
public class QuestionPoolTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static CreateQuestionDto Superlative(string text = "¿Quién se perdería en su propio barrio?") =>
        new() { Text = text, Type = QuestionType.Superlative };

    private async Task<(QuestionDto? Question, string? Error)> CreateAsync(int groupId, int userId, CreateQuestionDto dto)
    {
        await using var db = NewContext();
        return await new QuestionsService(db, TestMapper.Instance, Clock).CreateAsync(groupId, userId, dto);
    }

    private async Task AddPoolQuestionsAsync(int groupId, int creatorId, int count, bool isUsed = false)
    {
        await using var db = NewContext();
        db.Questions.AddRange(
            Enumerable
                .Range(0, count)
                .Select(i => new Question
                {
                    GroupId = groupId,
                    CreatorId = creatorId,
                    Type = QuestionType.Superlative,
                    Text = $"Pregunta {i}",
                    IsUsed = isUsed,
                })
        );
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task EachMemberCanKeepUpTo20UnusedQuestions()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await AddPoolQuestionsAsync(group.Id, group["ana"], 19);

        var twentieth = await CreateAsync(group.Id, group["ana"], Superlative());
        var twentyFirst = await CreateAsync(group.Id, group["ana"], Superlative());
        var someoneElse = await CreateAsync(group.Id, group["bea"], Superlative());

        twentieth.Error.Should().BeNull();
        twentieth.Question!.Text.Should().Be("¿Quién se perdería en su propio barrio?");
        // Player-facing messages follow the UI culture (rama 23): compare with the catalog.
        twentyFirst.Error.Should().Be(ServerText.UnusedQuestionsLimit(20));
        someoneElse.Error.Should().BeNull();
    }

    [Fact]
    public async Task QuestionsAlreadyAsked_DontCountTowardsTheQuota()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await AddPoolQuestionsAsync(group.Id, group["ana"], 25, isUsed: true);

        var (question, error) = await CreateAsync(group.Id, group["ana"], Superlative());

        error.Should().BeNull();
        question.Should().NotBeNull();
    }

    [Fact]
    public async Task TheGroupPoolIsCappedAt500()
    {
        var group = await SeedGroupAsync("ana", "bea");
        await AddPoolQuestionsAsync(group.Id, group["bea"], 500);

        var (question, error) = await CreateAsync(group.Id, group["ana"], Superlative());

        question.Should().BeNull();
        error.Should().Be(ServerText.GroupPoolFull);
        await using var db = NewContext();
        (await db.Questions.CountAsync()).Should().Be(500);
    }

    [Fact]
    public async Task ANewQuestionCanOnlyReferenceGroupMembers()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        var outsider = (await SeedGroupAsync("zoe"))["zoe"];

        var blacklistsSelf = await CreateAsync(
            group.Id,
            group["ana"],
            new()
            {
                Text = "¿Quién se perdería en su propio barrio?",
                Type = QuestionType.Superlative,
                BlacklistedUserIds = [group["ana"]],
            }
        );
        var outsiderInTeam = await CreateAsync(
            group.Id,
            group["ana"],
            new()
            {
                Text = "¿Qué equipo ganaría un concurso de karaoke?",
                Type = QuestionType.Deathmatch,
                Teams = [[group["ana"], group["bea"]], [group["carlos"], outsider]],
            }
        );

        blacklistsSelf.Error.Should().NotBeNull();
        outsiderInTeam.Error.Should().NotBeNull();
        await using var db = NewContext();
        (await db.Questions.AnyAsync()).Should().BeFalse();
    }
}
