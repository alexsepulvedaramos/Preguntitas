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
public class FlashController(IFlashService flashService, IGroupsService groupsService) : ControllerBase
{
    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId);
    }

    // POST api/groups/{groupId}/flash
    // Buys and launches a flash question at once (500 coins). 409 when coins are short or the
    // member already launched one today in this group.
    [HttpPost]
    public async Task<ActionResult<LaunchFlashResponseDto>> Launch(int groupId, CreateQuestionDto dto)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var (result, response) = await flashService.LaunchAsync(groupId, userId, dto);
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
            FlashResult.NotFound => NotFound(ServerText.FlashNotFound),
            FlashResult.AlreadyVoted => BadRequest(ServerText.AlreadyVotedThis),
            FlashResult.InvalidVote => BadRequest(ServerText.InvalidVote),
            _ => StatusCode(500),
        };
}
