// NudgesController.cs
// Toque / nudge (rama 43, §17.10): remind a member who hasn't voted an open question.

namespace VayaPreguntita.API.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VayaPreguntita.API.DTOs.Nudges;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Localization;
using VayaPreguntita.API.Services;

[Authorize]
[ApiController]
[Route("api/groups/{groupId}/nudges")]
public class NudgesController(INudgeService nudgeService, IGroupsService groupsService) : ControllerBase
{
    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId);
    }

    // GET api/groups/{groupId}/nudges/targets?flashId=
    // Who hasn't voted the question yet and whether a nudge reaches them. No flashId = today's.
    [HttpGet("targets")]
    public async Task<ActionResult<List<NudgeTargetDto>>> GetTargets(int groupId, [FromQuery] int? flashId)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var targets = await nudgeService.GetTargetsAsync(groupId, userId, flashId);
        return targets != null ? Ok(targets) : NotFound(ServerText.NoActiveQuestionToday);
    }

    // POST api/groups/{groupId}/nudges
    // Sends the nudge for CoinRewards.NudgePrice, or with one from the inventory (useOwned).
    [HttpPost]
    public async Task<ActionResult<SendNudgeResponseDto>> Send(int groupId, SendNudgeRequestDto dto)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var (result, response) = await nudgeService.SendAsync(groupId, userId, dto);
        return result switch
        {
            NudgeResult.Success => Ok(response),
            NudgeResult.NoOpenQuestion => NotFound(ServerText.NoActiveQuestionToday),
            NudgeResult.FlashNotFound => NotFound(ServerText.FlashNotFound),
            NudgeResult.InvalidTarget => BadRequest(ServerText.NudgeInvalidTarget),
            NudgeResult.AlreadyVoted => Conflict(ServerText.NudgeAlreadyVoted),
            NudgeResult.MessageTooLong => BadRequest(ServerText.NudgeMessageTooLong(NudgeRules.MaxMessageLength)),
            NudgeResult.Unreachable => Conflict(ServerText.NudgeUnreachable),
            NudgeResult.Recent => Conflict(ServerText.NudgeRecent),
            NudgeResult.NotEnoughCoins => Conflict(ServerText.NotEnoughCoins),
            NudgeResult.NoneOwned => Conflict(ServerText.NoItemOwned),
            _ => StatusCode(500),
        };
    }
}
