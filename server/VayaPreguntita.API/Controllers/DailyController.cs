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

[Authorize]
[ApiController]
[Route("api/groups/{groupId}/daily")]
public class DailyController(IDailyService dailyService, IGroupsService groupsService)
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

    // GET api/groups/{groupId}/daily/selection-sources
    // Preguntas que el selector puede elegir: pool del grupo + pack base
    [HttpGet("selection-sources")]
    public async Task<ActionResult<SelectionSourcesDto>> GetSelectionSources(int groupId)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var sources = await dailyService.GetSelectionSourcesAsync(groupId);
        return Ok(sources);
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
            SelectResult.QuestionNotFound => NotFound("Pregunta no encontrada en el pool."),
            SelectResult.InvalidQuestion => BadRequest(
                "La pregunta no es válida para este grupo."
            ),
            SelectResult.AlreadyActivated => BadRequest(
                "La pregunta ya está activa y no se puede cambiar."
            ),
            SelectResult.RecentlyUsedTemplate => BadRequest(
                "Esta pregunta del pack ya se usó recientemente. Elige otra."
            ),
            _ => StatusCode(500),
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

        var (result, results) = await dailyService.VoteAsync(groupId, userId, dto);

        return result switch
        {
            VoteResult.Success => Ok(results),
            VoteResult.NoActiveQuestion => NotFound("No hay pregunta activa hoy."),
            VoteResult.AlreadyVoted => BadRequest("Ya has votado hoy."),
            VoteResult.InvalidPayload => BadRequest("Voto inválido para este tipo de pregunta."),
            _ => StatusCode(500),
        };
    }
}
