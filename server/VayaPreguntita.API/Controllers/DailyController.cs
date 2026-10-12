// DailyController.cs
// Gestiona el flujo diario: estado actual, selección de pregunta y votación

namespace VayaPreguntita.API.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VayaPreguntita.API.DTOs.Daily;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Services;
using VayaPreguntita.API.Localization;

[Authorize]
[ApiController]
[Route("api/groups/{groupId}/daily")]
public class DailyController(
    IDailyService dailyService,
    IGroupsService groupsService,
    IStreakService streakService
)
    : ControllerBase
{
    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId);
    }

    // GET api/groups/{groupId}/daily/current
    // El frontend llama esto al entrar al grupo. La respuesta le dice qué mostrar.
    [HttpGet("current")]
    public async Task<ActionResult<DailyStatusDto>> GetCurrent(int groupId)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var status = await dailyService.GetCurrentStatusAsync(groupId, userId);
        return Ok(status);
    }

    // POST api/groups/{groupId}/daily/select
    // El selector del día elige (o cambia) la pregunta activa
    [HttpPost("select")]
    public async Task<ActionResult> SelectQuestion(int groupId, SelectQuestionDto dto)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var result = await dailyService.SelectQuestionAsync(groupId, userId, dto);

        return result switch
        {
            SelectResult.Success => Ok(),
            SelectResult.NotYourTurn => Forbid(),
            SelectResult.QuestionNotFound => NotFound(ServerText.QuestionNotInPool),
            SelectResult.InvalidQuestion => BadRequest(ServerText.QuestionNotValidForGroup),
            SelectResult.AlreadyActivated => BadRequest(ServerText.QuestionAlreadyActive),
            SelectResult.TemplateAlreadyUsed => BadRequest(ServerText.TemplateAlreadyUsed),
            _ => StatusCode(500),
        };
    }

    // POST api/groups/{groupId}/daily/select/confirm
    // The selector keeps the auto-selected question as their choice (§17.4)
    [HttpPost("select/confirm")]
    public async Task<ActionResult> ConfirmSelection(int groupId)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var result = await dailyService.ConfirmSelectionAsync(groupId, userId);
        return result == SelectResult.Success ? Ok() : Forbid();
    }

    // POST api/groups/{groupId}/daily/vote
    // El usuario envía su voto
    [HttpPost("vote")]
    public async Task<ActionResult> Vote(int groupId, CreateVoteDto dto)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var (result, response) = await dailyService.VoteAsync(groupId, userId, dto);

        return result switch
        {
            VoteResult.Success => Ok(response),
            VoteResult.NoActiveQuestion => NotFound(ServerText.NoActiveQuestionToday),
            VoteResult.AlreadyVoted => BadRequest(ServerText.AlreadyVoted),
            VoteResult.InvalidPayload => BadRequest(ServerText.InvalidVote),
            _ => StatusCode(500),
        };
    }

    // POST api/groups/{groupId}/daily/streak/dismiss-lost
    // Acknowledges the "Has perdido tu racha…" notice (rama 19).
    [HttpPost("streak/dismiss-lost")]
    public async Task<ActionResult> DismissLostStreak(int groupId)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        await streakService.DismissLostStreakAsync(groupId, userId);
        return NoContent();
    }

    // POST api/groups/{groupId}/daily/streak/dismiss-freeze
    // Acknowledges "Tu congelación salvó tu racha…" (rama 35).
    [HttpPost("streak/dismiss-freeze")]
    public async Task<ActionResult> DismissFreezeNotice(int groupId)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        await streakService.DismissFreezeNoticeAsync(groupId, userId);
        return NoContent();
    }

    // POST api/groups/{groupId}/daily/streak/dismiss-gift
    // Acknowledges "X te ha regalado una congelación" (rama 35).
    [HttpPost("streak/dismiss-gift")]
    public async Task<ActionResult> DismissGiftNotice(int groupId)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        await streakService.DismissGiftNoticeAsync(groupId, userId);
        return NoContent();
    }
}
