// PacksController.cs
// Gestiona los packs de preguntas disponibles para un grupo: listado, activación/desactivación
// por grupo (admin-only) y navegación paginada de sus templates.

namespace VayaPreguntita.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VayaPreguntita.API.DTOs.Packs;
using VayaPreguntita.API.Enums;
using VayaPreguntita.API.Extensions;
using VayaPreguntita.API.Services;

[Authorize]
[ApiController]
[Route("api/groups/{groupId}/packs")]
public class PacksController(IPacksService packsService, IGroupsService groupsService) : ControllerBase
{
    // GET api/groups/{groupId}/packs
    // Lista los packs con su estado de activación efectivo para este grupo
    [HttpGet]
    public async Task<ActionResult<List<PackDto>>> GetPacks(int groupId)
    {
        var userId = User.GetUserId();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var packs = await packsService.GetPacksForGroupAsync(groupId);
        return Ok(packs);
    }

    // PATCH api/groups/{groupId}/packs/{packId}
    // Activa o desactiva un pack para el grupo (admin-only)
    [HttpPatch("{packId}")]
    public async Task<IActionResult> SetPackEnabled(int groupId, int packId, SetPackEnabledDto dto)
    {
        var userId = User.GetUserId();
        if (!await groupsService.IsUserAdminAsync(userId, groupId))
            return Forbid();

        var (success, error) = await packsService.SetPackEnabledAsync(groupId, packId, dto.Enabled);
        return error switch
        {
            "not_found" => NotFound("El pack no existe."),
            "would_leave_zero_enabled" => BadRequest(
                "No se puede desactivar: el grupo debe tener al menos un pack activo."
            ),
            _ => NoContent(),
        };
    }

    // GET api/groups/{groupId}/packs/templates
    // Templates disponibles para el selector, paginados (cursor-based) y filtrables por tipo/pack
    [HttpGet("templates")]
    public async Task<ActionResult<PackTemplatePageDto>> GetTemplates(
        int groupId,
        [FromQuery] QuestionType? type = null,
        [FromQuery] int? packId = null,
        [FromQuery] int? before = null,
        [FromQuery] int pageSize = 12
    )
    {
        var userId = User.GetUserId();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        pageSize = Math.Min(pageSize, 50);
        var result = await packsService.GetTemplatesAsync(groupId, type, packId, before, pageSize);
        return Ok(result);
    }
}
