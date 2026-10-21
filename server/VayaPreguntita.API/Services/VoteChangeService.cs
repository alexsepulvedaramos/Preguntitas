using AutoMapper;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;

namespace VayaPreguntita.API.Services;

// "Change my vote" (rama 43, §17.10). The old answer is replaced, keeping its time, so the
// first-voter reward, the streak and the coins the vote earned stay exactly as they were.
// Late votes can't be changed: only open questions are, and a vote on one is never late.
public class VoteChangeService(
    AppDbContext context,
    IMapper mapper,
    ICoinService coinService,
    IInventoryService inventoryService,
    IFlashService flashService,
    TimeProvider clock
) : IVoteChangeService
{
    public async Task<(VoteChangeResult Result, ChangeVoteResponseDto? Response)> ChangeDailyAsync(
        int groupId,
        int userId,
        CreateVoteDto dto,
        bool useOwned
    )
    {
        var openEntry = await context
            .DailyEntries.Include(d => d.Question)
                .ThenInclude(q => q.Options)
            .Where(d => d.GroupId == groupId && d.ActivatedAt != null)
            .OrderByDescending(d => d.Date)
            .FirstOrDefaultAsync();
        if (openEntry == null)
            return (VoteChangeResult.NoOpenQuestion, null);

        var result = await ChangeAsync(groupId, userId, openEntry.Question, dto, useOwned);
        if (result != VoteChangeResult.Success)
            return (result, null);

        var votes = await context
            .Votes.Include(v => v.User)
            .Include(v => v.SelectedTargetUser)
            .Where(v => v.QuestionId == openEntry.QuestionId)
            .ToListAsync();
        return (
            result,
            new ChangeVoteResponseDto
            {
                Results = await ResultsBuilder.BuildAsync(context, openEntry.Question, votes, mapper),
                Balance = await coinService.GetBalanceAsync(userId),
                Inventory = await inventoryService.GetAsync(userId),
            }
        );
    }

    public async Task<(VoteChangeResult Result, ChangeVoteResponseDto? Response)> ChangeFlashAsync(
        int groupId,
        int userId,
        int flashId,
        CreateVoteDto dto,
        bool useOwned
    )
    {
        var flash = await context
            .FlashEntries.Include(f => f.Question)
                .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(f => f.Id == flashId && f.GroupId == groupId);
        if (flash == null)
            return (VoteChangeResult.FlashNotFound, null);
        if (flash.ClosesAt <= DailyClock.UtcNow(clock))
            return (VoteChangeResult.FlashClosed, null);

        var result = await ChangeAsync(groupId, userId, flash.Question, dto, useOwned);
        if (result != VoteChangeResult.Success)
            return (result, null);

        return (
            result,
            new ChangeVoteResponseDto
            {
                Flash = await flashService.GetStateAsync(flash.Id, userId),
                Balance = await coinService.GetBalanceAsync(userId),
                Inventory = await inventoryService.GetAsync(userId),
            }
        );
    }

    private async Task<VoteChangeResult> ChangeAsync(
        int groupId,
        int userId,
        Question question,
        CreateVoteDto dto,
        bool useOwned
    )
    {
        var current = await context
            .Votes.Where(v => v.QuestionId == question.Id && v.UserId == userId)
            .ToListAsync();
        if (current.Count == 0)
            return VoteChangeResult.NotVoted;

        if (await context.VoteChanges.AnyAsync(c => c.QuestionId == question.Id && c.UserId == userId))
            return VoteChangeResult.AlreadyChanged;

        var memberIds = await context
            .GroupMembers.Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .ToHashSetAsync();
        if (VotePayload.Validate(dto, question, memberIds) != null)
            return VoteChangeResult.InvalidVote;

        try
        {
            // Returning before the commit rolls everything back, the charge included.
            await using var transaction = await context.Database.BeginTransactionAsync();

            // The key (question, user) guards a double tap, coins or item alike.
            context.VoteChanges.Add(
                new VoteChange
                {
                    QuestionId = question.Id,
                    UserId = userId,
                    ChangedAt = DailyClock.UtcNow(clock),
                }
            );
            await context.SaveChangesAsync();

            if (useOwned)
            {
                if (!await inventoryService.TryConsumeAsync(userId, InventoryItem.VoteChange))
                    return VoteChangeResult.NoneOwned;
            }
            else if (
                !await coinService.SpendAsync(
                    userId,
                    CoinReason.VoteChange,
                    CoinRewards.VoteChangePrice,
                    groupId,
                    question.Id
                )
            )
                return VoteChangeResult.NotEnoughCoins;

            var respondedAt = current.Min(v => v.DateResponded);
            var votes = VotePayload.Build(dto, question, userId);
            votes.ForEach(v => v.DateResponded = respondedAt);
            context.Votes.RemoveRange(current);
            context.Votes.AddRange(votes);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();
            return VoteChangeResult.Success;
        }
        catch (DbUpdateException)
        {
            return VoteChangeResult.AlreadyChanged;
        }
    }
}
