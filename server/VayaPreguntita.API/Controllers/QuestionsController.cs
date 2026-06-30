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

[Authorize]
[ApiController]
[Route("api/groups/{groupId}/questions")]
public class QuestionsController(IQuestionsService questionsService, IGroupsService groupsService)
    : ControllerBase
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
        var result = await questionsService.GetPoolAsync(groupId, type, before, pageSize);
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
            date ?? DailyClock.Today()
        );
        if (result == null)
            return NotFound("No hay pregunta para esa fecha.");

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
        var result = await questionsService.GetHistoryAsync(groupId, before, pageSize);
        return Ok(result);
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
            "not_found" => NotFound("La pregunta no existe en este grupo."),
            "in_use" => BadRequest("No se puede eliminar una pregunta que ya se ha usado."),
            "forbidden" => Forbid(),
            _ => NoContent(),
        };
    }
}
