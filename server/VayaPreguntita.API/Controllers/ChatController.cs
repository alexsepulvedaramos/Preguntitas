using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VayaPreguntita.API.DTOs.Chat;
using VayaPreguntita.API.Extensions;
using VayaPreguntita.API.Services;

namespace VayaPreguntita.API.Controllers;

[ApiController]
[Route("api/groups/{groupId}/daily/chat")]
[Authorize]
public class ChatController(IChatService chatService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMessages(int groupId)
    {
        var userId = User.GetUserId();
        var messages = await chatService.GetMessagesAsync(groupId, userId);
        if (messages == null) return Forbid();
        return Ok(messages);
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage(int groupId, [FromBody] SendChatMessageDto dto)
    {
        var userId = User.GetUserId();
        var (success, error, messages) = await chatService.SendMessageAsync(groupId, userId, dto);
        if (!success) return BadRequest(new { error });
        return Ok(messages);
    }
}
