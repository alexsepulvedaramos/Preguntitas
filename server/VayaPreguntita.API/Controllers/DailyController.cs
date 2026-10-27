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
    IStreakService streakService,
    IAuthorGuessService authorGuessService,
    IVoteChangeService voteChangeService
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
    public async Task<ActionResult<SelectResponseDto>> SelectQuestion(int groupId, SelectQuestionDto dto)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var (result, coins) = await dailyService.SelectQuestionAsync(groupId, userId, dto);

        return result switch
        {
            SelectResult.Success => Ok(new SelectResponseDto { Coins = coins }),
            SelectResult.NotYourTurn => Forbid(),
            SelectResult.RaceLost => Conflict(ServerText.RaceLost),
            SelectResult.QuestionNotFound => NotFound(ServerText.QuestionNotInPool),
            SelectResult.InvalidQuestion => BadRequest(ServerText.QuestionNotValidForGroup),
            SelectResult.AlreadyActivated => BadRequest(ServerText.QuestionAlreadyActive),
            SelectResult.TemplateAlreadyUsed => BadRequest(ServerText.TemplateAlreadyUsed),
            _ => StatusCode(500),
        };
    }

    // POST api/groups/{groupId}/daily/select/confirm
    // The selector keeps the auto-selected question as their choice (§17.4); during the
    // race (§17.5) any member may, and the first one wins
    [HttpPost("select/confirm")]
    public async Task<ActionResult<SelectResponseDto>> ConfirmSelection(int groupId)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var (result, coins) = await dailyService.ConfirmSelectionAsync(groupId, userId);
        return result switch
        {
            SelectResult.Success => Ok(new SelectResponseDto { Coins = coins }),
            SelectResult.RaceLost => Conflict(ServerText.RaceLost),
            _ => Forbid(),
        };
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

    // PUT api/groups/{groupId}/daily/vote
    // "Change my vote" (rama 43, §17.10): redo your vote on today's open question, once, for
    // CoinRewards.VoteChangePrice. ?useOwned=true uses one from the inventory instead.
    [HttpPut("vote")]
    public async Task<ActionResult<ChangeVoteResponseDto>> ChangeVote(
        int groupId,
        CreateVoteDto dto,
        [FromQuery] bool useOwned = false
    )
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var (result, response) = await voteChangeService.ChangeDailyAsync(groupId, userId, dto, useOwned);
        return result == VoteChangeResult.Success ? Ok(response) : VoteChangeErrors.ToResult(this, result);
    }

    // POST api/groups/{groupId}/daily/guess
    // "¿Quién la escribió?" / "¿Quién la eligió?" (§17.6, §17.17): guess who wrote or chose the
    // open question, or skip.
    [HttpPost("guess")]
    public async Task<ActionResult<GuessAuthorResponseDto>> GuessAuthor(int groupId, GuessAuthorRequestDto dto)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var (result, response) = await authorGuessService.GuessAsync(groupId, userId, dto);
        return result == GuessResult.Success ? Ok(response) : GuessErrors.ToResult(this, result);
    }

    // POST api/groups/{groupId}/daily/guess/hint
    // Hint (§17.10): 20 coins, hides half of the wrong candidates. 409 if coins are short.
    // ?useOwned=true spends one from the inventory instead (rama 45); 409 when none is left.
    [HttpPost("guess/hint")]
    public async Task<ActionResult<AuthorHintResponseDto>> BuyAuthorHint(int groupId, [FromQuery] bool useOwned = false)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var (result, response) = await authorGuessService.BuyHintAsync(groupId, userId, useOwned);
        return result == GuessResult.Success ? Ok(response) : GuessErrors.ToResult(this, result);
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
