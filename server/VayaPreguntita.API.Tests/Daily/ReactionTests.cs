using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using VayaPreguntita.API.DTOs.Reactions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Services;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Daily;

// Reactions (rama 48, §17.15 item 6): on chat messages and on text a player wrote in the
// results, one per person and target (another emoji replaces yours), never on your own. Only a
// new reaction pushes the author, at most once every ReactionRules.PushCooldown.
[Collection(PostgresCollection.Name)]
public class ReactionTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    private sealed record Seeded(SeededGroup Group, int QuestionId, int BeaAnswerId, int BeaMessageId);

    // Today's Open Text question is open; ana and bea answered it, carlos hasn't. bea wrote in
    // the chat.
    private async Task<Seeded> SeedAsync()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        var questionId = await AddQuestionAsync(group.Id, QuestionType.OpenText, "¿Mejor recuerdo del viaje?", isUsed: true);
        var entryId = await AddEntryAsync(group.Id, Today, questionId, group["carlos"], activated: true);
        Clock.SetLocal(Today, DailyTime.AddHours(1));

        await using var db = NewContext();
        var answer = new Vote { QuestionId = questionId, UserId = group["bea"], FreeText = "El karaoke" };
        db.Votes.AddRange(answer, new Vote { QuestionId = questionId, UserId = group["ana"], FreeText = "La paella" });
        var message = new ChatMessage { DailyEntryId = entryId, UserId = group["bea"], Body = "Qué risa lo de ayer" };
        db.ChatMessages.Add(message);
        await db.SaveChangesAsync();
        return new Seeded(group, questionId, answer.Id, message.Id);
    }

    private async Task<(ReactionResult Result, List<ReactionDto>? Reactions)> OnAnswerAsync(
        Seeded s, string user, int voteId, string? emoji)
    {
        await using var db = NewContext();
        return await NewReactionService(db).SetOnAnswerAsync(s.Group.Id, s.Group[user], voteId, emoji);
    }

    private async Task<(ReactionResult Result, List<ReactionDto>? Reactions)> OnMessageAsync(
        Seeded s, string user, int messageId, string? emoji)
    {
        await using var db = NewContext();
        return await NewReactionService(db).SetOnMessageAsync(s.Group.Id, s.Group[user], messageId, emoji);
    }

    private void VerifyPushes(Times times) =>
        Notifications.Verify(
            n => n.SendReactionAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()),
            times);

    [Fact]
    public async Task ReactingToAnAnswer_ShowsInTheResults_AndPushesItsAuthor()
    {
        var s = await SeedAsync();

        var (result, reactions) = await OnAnswerAsync(s, "ana", s.BeaAnswerId, "fire");

        result.Should().Be(ReactionResult.Success);
        reactions.Should().ContainSingle(r => r.Emoji == "fire" && r.UserId == s.Group["ana"] && r.Username == "ana");
        Notifications.Verify(
            n => n.SendReactionAsync(
                s.Group.Id, s.Group["bea"], "Los de siempre", "ana", "🔥", true, "El karaoke", $"/groups/{s.Group.Id}"),
            Times.Once);

        await using var db = NewContext();
        var results = await new QuestionsService(db, TestMapper.Instance, Clock).GetByDateAsync(s.Group.Id, s.Group["ana"], Today);
        var bea = results!.FreeTextResponses.Single(r => r.UserId == s.Group["bea"]);
        bea.VoteId.Should().Be(s.BeaAnswerId);
        bea.Reactions.Should().ContainSingle(r => r.Emoji == "fire");
        results.FreeTextResponses.Single(r => r.UserId == s.Group["ana"]).Reactions.Should().BeEmpty();
    }

    [Fact]
    public async Task AnotherEmoji_ReplacesYours_WithoutAnotherPush_AndNullRemovesIt()
    {
        var s = await SeedAsync();

        await OnAnswerAsync(s, "ana", s.BeaAnswerId, "fire");
        var (_, changed) = await OnAnswerAsync(s, "ana", s.BeaAnswerId, "upside");
        changed.Should().ContainSingle().Which.Emoji.Should().Be("upside");

        Clock.Advance(ReactionRules.PushCooldown * 2);
        await OnAnswerAsync(s, "ana", s.BeaAnswerId, "laugh");
        VerifyPushes(Times.Once());

        var (result, removed) = await OnAnswerAsync(s, "ana", s.BeaAnswerId, null);
        result.Should().Be(ReactionResult.Success);
        removed.Should().BeEmpty();
        await using var db = NewContext();
        (await db.Reactions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AtMostOnePushPerAuthor_EveryCooldown()
    {
        var s = await SeedAsync();
        await using (var db = NewContext())
        {
            db.Votes.Add(new Vote { QuestionId = s.QuestionId, UserId = s.Group["carlos"], FreeText = "Todo" });
            await db.SaveChangesAsync();
        }

        await OnAnswerAsync(s, "ana", s.BeaAnswerId, "fire");
        await OnMessageAsync(s, "carlos", s.BeaMessageId, "laugh");
        (await OnAnswerAsync(s, "carlos", s.BeaAnswerId, "love")).Result.Should().Be(ReactionResult.Success);
        VerifyPushes(Times.Once());

        Clock.Advance(ReactionRules.PushCooldown + TimeSpan.FromSeconds(1));
        await OnMessageAsync(s, "ana", s.BeaMessageId, "wow");
        VerifyPushes(Times.Exactly(2));
    }

    [Fact]
    public async Task NotOnYourOwn_NorAnInvalidEmoji_NorBeforeVoting()
    {
        var s = await SeedAsync();

        (await OnAnswerAsync(s, "bea", s.BeaAnswerId, "fire")).Result.Should().Be(ReactionResult.OwnContent);
        (await OnMessageAsync(s, "bea", s.BeaMessageId, "fire")).Result.Should().Be(ReactionResult.OwnContent);
        (await OnAnswerAsync(s, "ana", s.BeaAnswerId, "🔥")).Result.Should().Be(ReactionResult.InvalidEmoji);
        // carlos hasn't voted, so he can't see the answers yet (§17.7).
        (await OnAnswerAsync(s, "carlos", s.BeaAnswerId, "fire")).Result.Should().Be(ReactionResult.Locked);
        VerifyPushes(Times.Never());
    }

    [Fact]
    public async Task OnlyPlayerWrittenText_AndOnlyInThisGroup()
    {
        var s = await SeedAsync();
        var other = await SeedGroupAsync("dani", "eva");
        int structuredVoteId, systemMessageId;
        await using (var db = NewContext())
        {
            var superlativeId = await AddQuestionAsync(s.Group.Id, isUsed: true);
            var structured = new Vote { QuestionId = superlativeId, UserId = s.Group["bea"], SelectedTargetUserId = s.Group["ana"] };
            db.Votes.Add(structured);
            var entryId = await db.DailyEntries.Where(d => d.GroupId == s.Group.Id).Select(d => d.Id).SingleAsync();
            var system = new ChatMessage { DailyEntryId = entryId, UserId = null, Body = "racha" };
            db.ChatMessages.Add(system);
            await db.SaveChangesAsync();
            (structuredVoteId, systemMessageId) = (structured.Id, system.Id);
        }

        (await OnAnswerAsync(s, "ana", structuredVoteId, "fire")).Result.Should().Be(ReactionResult.NotFound);
        (await OnMessageAsync(s, "ana", systemMessageId, "fire")).Result.Should().Be(ReactionResult.NotFound);
        await using (var db = NewContext())
        {
            var service = NewReactionService(db);
            (await service.SetOnAnswerAsync(other.Id, other["dani"], s.BeaAnswerId, "fire")).Result
                .Should().Be(ReactionResult.NotFound);
            (await service.SetOnMessageAsync(other.Id, other["dani"], s.BeaMessageId, "fire")).Result
                .Should().Be(ReactionResult.NotFound);
        }
    }

    [Fact]
    public async Task ChatMessages_CarryTheirReactions()
    {
        var s = await SeedAsync();

        await OnMessageAsync(s, "ana", s.BeaMessageId, "laugh");
        await OnMessageAsync(s, "carlos", s.BeaMessageId, "laugh");

        await using var db = NewContext();
        var messages = await new ChatService(db, Notifications.Object).GetMessagesAsync(s.Group.Id, s.Group["bea"]);
        messages!.Single().Reactions.Select(r => r.Username).Should().Equal("ana", "carlos");
        Notifications.Verify(
            n => n.SendReactionAsync(
                s.Group.Id, s.Group["bea"], "Los de siempre", "ana", "😂", false, "Qué risa lo de ayer", $"/groups/{s.Group.Id}"),
            Times.Once);
    }

    [Fact]
    public async Task AClosedAnswer_PushOpensTheHistory()
    {
        var s = await SeedAsync();
        await AddActivatedDayAsync(s.Group.Id, Today.AddDays(1), s.Group["ana"]);

        await OnAnswerAsync(s, "ana", s.BeaAnswerId, "love");

        Notifications.Verify(
            n => n.SendReactionAsync(
                s.Group.Id, s.Group["bea"], It.IsAny<string>(), "ana", "❤️", true, "El karaoke", $"/groups/{s.Group.Id}/history"),
            Times.Once);
    }
}
