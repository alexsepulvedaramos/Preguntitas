// QuestionsController.cs
// Gestiona el pool de preguntas del grupo (crear, listar)

namespace VayaPreguntita.API.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VayaPreguntita.API.DTOs.Questions;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Services;
using VayaPreguntita.API.Localization;

[Authorize]
[ApiController]
[Route("api/groups/{groupId}/questions")]
public class QuestionsController(
    IQuestionsService questionsService,
    IGroupsService groupsService,
    IDailyService dailyService,
    TimeProvider clock
) : ControllerBase
{
    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId);
    }

    // GET api/groups/{groupId}/questions/pool
    // Lista paginada de las preguntas del pool que aún no han salido (cursor-based, más
    // recientes primero). `before` es el último Id visto; omitir para la primera página.
    [HttpGet("pool")]
    public async Task<ActionResult<QuestionPageDto>> GetPool(
        int groupId,
        [FromQuery] QuestionType? type = null,
        [FromQuery] int? before = null,
        [FromQuery] int pageSize = 12
    )
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        pageSize = Math.Min(pageSize, 50);
        var result = await questionsService.GetPoolAsync(groupId, userId, type, before, pageSize);
        return Ok(result);
    }

    // GET api/groups/{groupId}/questions?date=2026-06-20
    // Histórico: devuelve la pregunta y resultados de una fecha concreta
    [HttpGet]
    public async Task<ActionResult<QuestionResultDto>> GetByDate(
        int groupId,
        [FromQuery] DateOnly? date
    )
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var result = await questionsService.GetByDateAsync(
            groupId,
            userId,
            date ?? DailyClock.Today(clock)
        );
        if (result == null)
            return NotFound(ServerText.NoQuestionForDate);

        return Ok(result);
    }

    // POST api/groups/{groupId}/questions
    // Añade una pregunta al pool
    [HttpPost]
    public async Task<ActionResult<QuestionDto>> CreateQuestion(int groupId, CreateQuestionDto dto)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var (question, error) = await questionsService.CreateAsync(groupId, userId, dto);
        if (error != null)
            return BadRequest(error);

        return CreatedAtAction(nameof(GetPool), new { groupId }, question);
    }

    // GET api/groups/{groupId}/history
    // Historial paginado de preguntas ya cerradas (cursor-based, más recientes primero).
    // `before` es la fecha del último ítem visto (YYYY-MM-DD); omitir para la primera página.
    [HttpGet("/api/groups/{groupId}/history")]
    public async Task<ActionResult<HistoryPageDto>> GetHistory(
        int groupId,
        [FromQuery] DateOnly? before = null,
        [FromQuery] int pageSize = 20
    )
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        pageSize = Math.Min(pageSize, 50);
        var result = await questionsService.GetHistoryAsync(groupId, userId, before, pageSize);
        return Ok(result);
    }

    // POST api/groups/{groupId}/history/{date}/vote
    // Voto tardío desde el historial (§17.7): cuenta en el recuento pero no para rachas,
    // monedas ni premios. Devuelve el detalle ya desbloqueado.
    [HttpPost("/api/groups/{groupId}/history/{date}/vote")]
    public async Task<ActionResult<QuestionResultDto>> LateVote(int groupId, DateOnly date, CreateVoteDto dto)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var result = await dailyService.LateVoteAsync(groupId, userId, date, dto);
        return result switch
        {
            VoteResult.Success => Ok(await questionsService.GetByDateAsync(groupId, userId, date)),
            VoteResult.QuestionNotFound => NotFound(ServerText.NoQuestionForDate),
            VoteResult.QuestionNotClosed => BadRequest(ServerText.LateVoteNotClosed),
            VoteResult.AlreadyVoted => BadRequest(ServerText.AlreadyVotedThis),
            VoteResult.InvalidPayload => BadRequest(ServerText.InvalidVote),
            _ => StatusCode(500),
        };
    }

    // DELETE api/groups/{groupId}/questions/{id}
    // Elimina una pregunta del pool: solo el creador o el admin del grupo, y solo si no se ha usado
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuestion(int groupId, int id)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var error = await questionsService.DeleteAsync(groupId, userId, id);
        return error switch
        {
            "not_found" => NotFound(ServerText.QuestionNotInGroup),
            "in_use" => BadRequest(ServerText.QuestionAlreadyUsed),
            "forbidden" => Forbid(),
            _ => NoContent(),
        };
    }
}
