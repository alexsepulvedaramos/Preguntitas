using FluentAssertions;
using Moq;
using VayaPreguntita.API.DTOs.Chat;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Services;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Daily;

// Chat only after you vote (rama 49): the open question's chat is locked — only its message
// count shows — until you've voted it; you can't write in it before, and new-message pushes
// only go to members who have voted (the held-back ones are dropped, not summarised).
[Collection(PostgresCollection.Name)]
public class ChatAfterVoteTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    // ana and bea voted today's question and wrote in the chat; carlos hasn't voted.
    private async Task<(SeededGroup Group, int QuestionId)> SeedAsync()
    {
        var group = await SeedGroupAsync("ana", "bea", "carlos");
        var (entryId, questionId) = await AddActivatedDayAsync(group.Id, Today, group["ana"]);
        Clock.SetLocal(Today, DailyTime.AddHours(1));

        await using var db = NewContext();
        db.Votes.AddRange(
            new Vote { QuestionId = questionId, UserId = group["ana"], SelectedTargetUserId = group["bea"] },
            new Vote { QuestionId = questionId, UserId = group["bea"], SelectedTargetUserId = group["ana"] });
        db.ChatMessages.AddRange(
            new ChatMessage { DailyEntryId = entryId, UserId = group["ana"], Body = "Era obvio" },
            new ChatMessage { DailyEntryId = entryId, UserId = group["bea"], Body = "Qué dices", CreatedAt = DateTime.UtcNow.AddSeconds(1) });
        await db.SaveChangesAsync();
        return (group, questionId);
    }

    private async Task<ChatDto?> ChatAsync(int groupId, int userId)
    {
        await using var db = NewContext();
        return await new ChatService(db, Notifications.Object).GetMessagesAsync(groupId, userId);
    }

    private async Task<(bool Success, string? Error)> SendAsync(int groupId, int userId, string body)
    {
        await using var db = NewContext();
        var (success, error, _) = await new ChatService(db, Notifications.Object)
            .SendMessageAsync(groupId, userId, new SendChatMessageDto { Body = body });
        return (success, error);
    }

    [Fact]
    public async Task BeforeVoting_TheChatIsLocked_AndOnlyShowsTheCount()
    {
        var (group, _) = await SeedAsync();

        var chat = await ChatAsync(group.Id, group["carlos"]);

        chat!.Locked.Should().BeTrue();
        chat.MessageCount.Should().Be(2);
        chat.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task AfterVoting_TheWholeChatShows()
    {
        var (group, questionId) = await SeedAsync();
        await using (var db = NewContext())
        {
            db.Votes.Add(new Vote { QuestionId = questionId, UserId = group["carlos"], SelectedTargetUserId = group["ana"] });
            await db.SaveChangesAsync();
        }

        var chat = await ChatAsync(group.Id, group["carlos"]);

        chat!.Locked.Should().BeFalse();
        chat.MessageCount.Should().Be(2);
        chat.Messages.Select(m => m.Body).Should().Equal("Era obvio", "Qué dices");
    }

    [Fact]
    public async Task BeforeVoting_YouCantWrite()
    {
        var (group, _) = await SeedAsync();

        var (success, error) = await SendAsync(group.Id, group["carlos"], "¿Qué ha votado la gente?");

        success.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
        (await ChatAsync(group.Id, group["ana"]))!.Messages.Should().HaveCount(2);
        Notifications.Verify(
            n => n.SendNewMessageAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task AVoter_Writes_AndThePushKnowsWhichQuestion()
    {
        var (group, questionId) = await SeedAsync();

        (await SendAsync(group.Id, group["ana"], "Bea, confiesa")).Success.Should().BeTrue();

        Notifications.Verify(
            n => n.SendNewMessageAsync(
                group.Id, questionId, group["ana"], It.IsAny<string>(), "ana", "Bea, confiesa"),
            Times.Once);
    }

    [Fact]
    public async Task WithNoOpenQuestion_TheChatIsEmptyButNotLocked()
    {
        var group = await SeedGroupAsync("ana", "bea");

        var chat = await ChatAsync(group.Id, group["ana"]);

        chat!.Locked.Should().BeFalse();
        chat.Messages.Should().BeEmpty();
    }
}
