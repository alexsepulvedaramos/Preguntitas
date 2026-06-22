// QuestionsController.cs
// Gestiona el pool de preguntas del grupo (crear, listar)

namespace VayaPreguntita.API.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VayaPreguntita.API.DTOs.Questions;
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
    // Lista las preguntas del pool que aún no han salido
    [HttpGet("pool")]
    public async Task<ActionResult<IEnumerable<QuestionDto>>> GetPool(int groupId)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var questions = await questionsService.GetPoolAsync(groupId);
        return Ok(questions);
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
}
