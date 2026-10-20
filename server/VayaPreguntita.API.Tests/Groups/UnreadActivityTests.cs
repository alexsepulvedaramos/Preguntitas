using FluentAssertions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Tests.Testing;

namespace VayaPreguntita.API.Tests.Groups;

// The unread-activity dot on the group list (§16): something new since the member last
// opened the group — others' chat messages, a question still to vote, new votes on one they
// answered, or their turn to pick.
[Collection(PostgresCollection.Name)]
public class UnreadActivityTests(DatabaseFixture fixture) : DomainTestBase(fixture)
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    private async Task<bool> HasActivityAsync(int userId, int groupId)
    {
        await using var db = NewContext();
        return (await NewGroupsService(db).GetUserGroupsAsync(userId)).Single(g => g.Id == groupId).HasNewActivity;
    }

    private async Task MarkSeenAsync(int userId, int groupId)
    {
        await using var db = NewContext();
        (await NewGroupsService(db).MarkSeenAsync(userId, groupId)).Should().BeTrue();
    }

    [Fact]
    public async Task ANewQuestion_LightsTheDot_UntilTheGroupIsOpened()
    {
        var group = await SeedGroupAsync("ana", "bea");
        var questionId = await AddQuestionAsync(group.Id);
        await AddEntryAsync(group.Id, Today, questionId, group["bea"], activated: true);
        Clock.SetLocal(Today, DailyTime.AddHours(1));

        (await HasActivityAsync(group["ana"], group.Id)).Should().BeTrue();

        await MarkSeenAsync(group["ana"], group.Id);
        (await HasActivityAsync(group["ana"], group.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task SomeoneElsesChatMessage_LightsTheDot_ButYourOwnDoesNot()
    {
        var group = await SeedGroupAsync("ana", "bea");
        var questionId = await AddQuestionAsync(group.Id);
        var entryId = await AddEntryAsync(group.Id, Today, questionId, group["bea"], activated: true);
        Clock.SetLocal(Today, DailyTime.AddHours(1));
        await MarkSeenAsync(group["ana"], group.Id);

        Clock.Advance(TimeSpan.FromMinutes(5));
        await AddChatAsync(entryId, group["ana"]);
        (await HasActivityAsync(group["ana"], group.Id)).Should().BeFalse();

        await AddChatAsync(entryId, group["bea"]);
        (await HasActivityAsync(group["ana"], group.Id)).Should().BeTrue();
    }

    private async Task AddChatAsync(int entryId, int userId)
    {
        await using var db = NewContext();
        db.ChatMessages.Add(new ChatMessage { DailyEntryId = entryId, UserId = userId, Body = "hola", CreatedAt = Clock.UtcNow });
        await db.SaveChangesAsync();
    }
}
