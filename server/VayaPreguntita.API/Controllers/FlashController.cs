// FlashController.cs
// Flash questions (§17.11): launch one for coins, vote it, and read it from the history.

namespace VayaPreguntita.API.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Localization;
using VayaPreguntita.API.Services;

[Authorize]
[ApiController]
[Route("api/groups/{groupId}/flash")]
public class FlashController(
    IFlashService flashService,
    IGroupsService groupsService,
    IVoteChangeService voteChangeService
) : ControllerBase
{
    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId);
    }

    // POST api/groups/{groupId}/flash
    // Buys and launches a flash question at once (CoinRewards.FlashQuestionPrice). 409 when coins are short or the
    // member already launched one today in this group. ?useOwned=true launches it with one from
    // the inventory instead of paying (rama 45); 409 when none is left. Add-ons (rama 50), paid
    // in coins on top: ?anonymousAuthor=true, ?anonymousVotes=true and ?capsuleOpensOn=yyyy-MM-dd
    // (1 day to 1 year ahead).
    [HttpPost]
    public async Task<ActionResult<LaunchFlashResponseDto>> Launch(
        int groupId,
        CreateQuestionDto dto,
        [FromQuery] bool useOwned = false,
        [FromQuery] bool anonymousAuthor = false,
        [FromQuery] bool anonymousVotes = false,
        [FromQuery] DateOnly? capsuleOpensOn = null
    )
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var (result, response) = await flashService.LaunchAsync(
            groupId, userId, dto, useOwned, new FlashOptions(anonymousAuthor, anonymousVotes, capsuleOpensOn));
        return result == FlashResult.Success ? Ok(response) : Error(result);
    }

    // POST api/groups/{groupId}/flash/{flashId}/extend
    // +24 h on an open flash (rama 50), once, by any member. 409 when it was already extended
    // or coins are short, 400 once it has closed.
    [HttpPost("{flashId:int}/extend")]
    public async Task<ActionResult<ExtendFlashResponseDto>> Extend(int groupId, int flashId)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var (result, response) = await flashService.ExtendAsync(groupId, userId, flashId);
        return result == FlashResult.Success ? Ok(response) : Error(result);
    }

    // POST api/groups/{groupId}/flash/{flashId}/vote
    // On time while it's open (returns the fresh state); a late vote from the history once
    // it has closed (204).
    [HttpPost("{flashId:int}/vote")]
    public async Task<ActionResult<FlashStateDto>> Vote(int groupId, int flashId, CreateVoteDto dto)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var (result, state) = await flashService.VoteAsync(groupId, userId, flashId, dto);
        if (result != FlashResult.Success)
            return Error(result);
        return state != null ? Ok(state) : NoContent();
    }

    // PUT api/groups/{groupId}/flash/{flashId}/vote
    // "Change my vote" (rama 43, §17.10) on an open flash, once. ?useOwned=true uses one from
    // the inventory instead of paying.
    [HttpPut("{flashId:int}/vote")]
    public async Task<ActionResult<ChangeVoteResponseDto>> ChangeVote(
        int groupId,
        int flashId,
        CreateVoteDto dto,
        [FromQuery] bool useOwned = false
    )
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var (result, response) = await voteChangeService.ChangeFlashAsync(groupId, userId, flashId, dto, useOwned);
        return result == VoteChangeResult.Success ? Ok(response) : VoteChangeErrors.ToResult(this, result);
    }

    // GET api/groups/{groupId}/flash/{flashId}
    // History detail, gated like a daily question (§17.7).
    [HttpGet("{flashId:int}")]
    public async Task<ActionResult<QuestionResultDto>> GetDetail(int groupId, int flashId)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var detail = await flashService.GetDetailAsync(groupId, userId, flashId);
        return detail != null ? Ok(detail) : NotFound(ServerText.FlashNotFound);
    }

    private ActionResult Error(FlashResult result) =>
        result switch
        {
            FlashResult.GroupNotStarted => BadRequest(ServerText.FlashGroupNotStarted),
            FlashResult.InvalidQuestion => BadRequest(ServerText.QuestionNotValidForGroup),
            FlashResult.DailyLimitReached => Conflict(ServerText.FlashDailyLimit),
            FlashResult.NotEnoughCoins => Conflict(ServerText.NotEnoughCoins),
            FlashResult.NoneOwned => Conflict(ServerText.NoItemOwned),
            FlashResult.NotFound => NotFound(ServerText.FlashNotFound),
            FlashResult.AlreadyVoted => BadRequest(ServerText.AlreadyVotedThis),
            FlashResult.InvalidVote => BadRequest(ServerText.InvalidVote),
            FlashResult.InvalidCapsuleDate => BadRequest(ServerText.FlashCapsuleDateInvalid),
            FlashResult.Closed => BadRequest(ServerText.FlashClosed),
            FlashResult.AlreadyExtended => Conflict(ServerText.FlashAlreadyExtended),
            _ => StatusCode(500),
        };
}
