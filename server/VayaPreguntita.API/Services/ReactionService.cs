using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Reactions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

// Reactions (rama 48, §17.15 item 6). Free; one per person and target (another emoji replaces
// yours); never on your own message or answer. Only a new reaction pushes its author, at most
// once every ReactionRules.PushCooldown — changing or removing one never does.
public class ReactionService(
    AppDbContext context,
    INotificationService notificationService,
    TimeProvider clock
) : IReactionService
{
    private sealed record Target(int AuthorUserId, string Text, string Url);

    public async Task<(ReactionResult Result, List<ReactionDto>? Reactions)> SetOnMessageAsync(
        int groupId, int userId, int messageId, string? emoji)
    {
        var message = await context
            .ChatMessages.Where(m => m.Id == messageId && m.DailyEntry.GroupId == groupId && m.UserId != null)
            .Select(m => new { AuthorUserId = m.UserId!.Value, m.Body, m.DailyEntry.QuestionId })
            .FirstOrDefaultAsync();
        if (message == null)
            return (ReactionResult.NotFound, null);

        // The day's chat is only readable once you've voted its question (rama 49).
        if (message.AuthorUserId != userId
            && !await context.Votes.AnyAsync(v => v.QuestionId == message.QuestionId && v.UserId == userId))
            return (ReactionResult.Locked, null);

        var target = new Target(message.AuthorUserId, message.Body, $"/groups/{groupId}");
        var result = await SetAsync(groupId, userId, target, emoji, isAnswer: false,
            r => r.ChatMessageId == messageId, r => r.ChatMessageId = messageId);
        return (result, result == ReactionResult.Success ? await ListAsync(r => r.ChatMessageId == messageId) : null);
    }

    public async Task<(ReactionResult Result, List<ReactionDto>? Reactions)> SetOnAnswerAsync(
        int groupId, int userId, int voteId, string? emoji)
    {
        // Only text a player wrote: Open Text answers and a poll's "Otro".
        var vote = await context
            .Votes.Where(v => v.Id == voteId && v.Question.GroupId == groupId && v.FreeText != null && v.FreeText != "")
            .Select(v => new { v.UserId, v.QuestionId, v.FreeText })
            .FirstOrDefaultAsync();
        if (vote == null)
            return (ReactionResult.NotFound, null);

        // Answers are only visible once you've voted the question (§17.7), and not at all while
        // a time capsule is sealed (rama 50).
        if (vote.UserId != userId
            && !await context.Votes.AnyAsync(v => v.QuestionId == vote.QuestionId && v.UserId == userId))
            return (ReactionResult.Locked, null);
        var now = DailyClock.UtcNow(clock);
        if (await context.FlashEntries.AnyAsync(f => f.QuestionId == vote.QuestionId && f.ResultsHiddenUntil > now))
            return (ReactionResult.Locked, null);

        var target = new Target(vote.UserId, vote.FreeText!, await AnswerUrlAsync(groupId, vote.QuestionId));
        var result = await SetAsync(groupId, userId, target, emoji, isAnswer: true,
            r => r.VoteId == voteId, r => r.VoteId = voteId);
        return (result, result == ReactionResult.Success ? await ListAsync(r => r.VoteId == voteId) : null);
    }

    private async Task<ReactionResult> SetAsync(
        int groupId,
        int userId,
        Target target,
        string? emoji,
        bool isAnswer,
        System.Linq.Expressions.Expression<Func<Reaction, bool>> onTarget,
        Action<Reaction> attach)
    {
        if (target.AuthorUserId == userId)
            return ReactionResult.OwnContent;
        if (emoji != null && !ReactionRules.IsValid(emoji))
            return ReactionResult.InvalidEmoji;

        var existing = await context.Reactions.Where(onTarget).FirstOrDefaultAsync(r => r.UserId == userId);
        if (emoji == null)
        {
            if (existing != null)
            {
                context.Reactions.Remove(existing);
                await context.SaveChangesAsync();
            }
            return ReactionResult.Success;
        }

        if (existing != null)
        {
            existing.Emoji = emoji;
            await context.SaveChangesAsync();
            return ReactionResult.Success;
        }

        var now = DailyClock.UtcNow(clock);
        var since = now - ReactionRules.PushCooldown;
        var pushedRecently = await context.Reactions.AnyAsync(r =>
            r.AuthorUserId == target.AuthorUserId && r.NotifiedAt > since);

        var reaction = new Reaction
        {
            UserId = userId,
            AuthorUserId = target.AuthorUserId,
            Emoji = emoji,
            CreatedAt = now,
            NotifiedAt = pushedRecently ? null : now,
        };
        attach(reaction);
        context.Reactions.Add(reaction);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // A double tap raced us to the (target, user) row: keep that one, with this emoji.
            context.ChangeTracker.Clear();
            var raced = await context.Reactions.Where(onTarget).FirstOrDefaultAsync(r => r.UserId == userId);
            if (raced == null)
                throw;
            raced.Emoji = emoji;
            await context.SaveChangesAsync();
            return ReactionResult.Success;
        }

        if (!pushedRecently)
        {
            var groupName = await context.Groups.Where(g => g.Id == groupId).Select(g => g.Name).FirstAsync();
            var reactor = await context.Users.Where(u => u.Id == userId).Select(u => u.Username).FirstAsync();
            _ = notificationService.SendReactionAsync(
                groupId, target.AuthorUserId, groupName, reactor, ReactionRules.Emojis[emoji],
                isAnswer, target.Text, target.Url);
        }
        return ReactionResult.Success;
    }

    // An answer to the open daily question or an open flash is read on the group page; any
    // other one in the history.
    private async Task<string> AnswerUrlAsync(int groupId, int questionId)
    {
        var openDailyQuestionId = await context
            .DailyEntries.Where(d => d.GroupId == groupId && d.ActivatedAt != null)
            .OrderByDescending(d => d.Date)
            .Select(d => (int?)d.QuestionId)
            .FirstOrDefaultAsync();
        var now = DailyClock.UtcNow(clock);
        var isOpen = openDailyQuestionId == questionId
            || await context.FlashEntries.AnyAsync(f => f.QuestionId == questionId && f.ClosesAt > now);
        return isOpen ? $"/groups/{groupId}" : $"/groups/{groupId}/history";
    }

    private async Task<List<ReactionDto>> ListAsync(System.Linq.Expressions.Expression<Func<Reaction, bool>> onTarget) =>
        await context
            .Reactions.Where(onTarget)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new ReactionDto { Emoji = r.Emoji, UserId = r.UserId, Username = r.User.Username })
            .ToListAsync();
}
