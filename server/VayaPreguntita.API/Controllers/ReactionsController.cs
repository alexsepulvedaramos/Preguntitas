// ReactionsController.cs
// Reactions (rama 48, §17.15 item 6) on chat messages and player-written answers. They are read
// with the chat and the results, so there is nothing to poll here: only set or remove yours.

namespace VayaPreguntita.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VayaPreguntita.API.DTOs.Reactions;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Extensions;
using VayaPreguntita.API.Localization;
using VayaPreguntita.API.Services;

[Authorize]
[ApiController]
[Route("api/groups/{groupId}/reactions")]
public class ReactionsController(IReactionService reactionService, IGroupsService groupsService) : ControllerBase
{
    // PUT api/groups/{groupId}/reactions/messages/{messageId}
    [HttpPut("messages/{messageId}")]
    public async Task<ActionResult<List<ReactionDto>>> SetOnMessage(int groupId, int messageId, SetReactionDto dto)
    {
        var userId = User.GetUserId();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        return ToResponse(await reactionService.SetOnMessageAsync(groupId, userId, messageId, dto.Emoji));
    }

    // PUT api/groups/{groupId}/reactions/answers/{voteId}
    [HttpPut("answers/{voteId}")]
    public async Task<ActionResult<List<ReactionDto>>> SetOnAnswer(int groupId, int voteId, SetReactionDto dto)
    {
        var userId = User.GetUserId();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        return ToResponse(await reactionService.SetOnAnswerAsync(groupId, userId, voteId, dto.Emoji));
    }

    private ActionResult<List<ReactionDto>> ToResponse((ReactionResult Result, List<ReactionDto>? Reactions) outcome) =>
        outcome.Result switch
        {
            ReactionResult.Success => Ok(outcome.Reactions),
            ReactionResult.NotFound => NotFound(ServerText.ReactionTargetNotFound),
            ReactionResult.InvalidEmoji => BadRequest(ServerText.ReactionInvalidEmoji),
            ReactionResult.OwnContent => BadRequest(ServerText.ReactionOwnContent),
            ReactionResult.Locked => Conflict(ServerText.ReactionLocked),
            _ => StatusCode(500),
        };
}
