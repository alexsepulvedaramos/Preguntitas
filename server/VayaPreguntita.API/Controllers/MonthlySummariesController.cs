// MonthlySummariesController.cs
// Monthly summaries ("wrapped", §17.12): the months a group has and one month's story.

namespace VayaPreguntita.API.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VayaPreguntita.API.DTOs.MonthlySummaries;
using VayaPreguntita.API.Localization;
using VayaPreguntita.API.Services;

[Authorize]
[ApiController]
[Route("api/groups/{groupId}/monthly-summaries")]
public class MonthlySummariesController(
    IMonthlySummaryService summaryService,
    IGroupsService groupsService
) : ControllerBase
{
    private bool TryGetCurrentUserId(out int userId)
    {
        userId = 0;
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out userId);
    }

    // GET api/groups/{groupId}/monthly-summaries
    // The last 12 months that have a summary, newest first.
    [HttpGet]
    public async Task<ActionResult<List<MonthlySummaryListItemDto>>> List(int groupId)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        return Ok(await summaryService.ListAsync(groupId));
    }

    // GET api/groups/{groupId}/monthly-summaries/{month}   (month = YYYY-MM)
    // The caller's own match is the only personal part sent.
    [HttpGet("{month}")]
    public async Task<ActionResult<MonthlySummaryDto>> Get(int groupId, string month)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();
        if (!await groupsService.IsUserInGroupAsync(userId, groupId))
            return Forbid();

        var summary = await summaryService.GetAsync(groupId, userId, month);
        return summary != null ? Ok(summary) : NotFound(ServerText.MonthlySummaryNotFound);
    }
}
