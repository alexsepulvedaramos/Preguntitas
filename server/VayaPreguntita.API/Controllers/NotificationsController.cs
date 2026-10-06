using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Notifications;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Extensions;
using VayaPreguntita.API.Options;

namespace VayaPreguntita.API.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController(AppDbContext context, IOptions<VapidOptions> vapidOptions) : ControllerBase
{
    [HttpGet("vapid-key")]
    [AllowAnonymous]
    public IActionResult GetVapidPublicKey() =>
        Ok(new { publicKey = vapidOptions.Value.PublicKey });

    [HttpPost("subscriptions")]
    [Authorize]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto dto)
    {
        var userId = User.GetUserId();

        // Upsert by endpoint — same device re-subscribing after key rotation.
        var existing = await context.DevicePushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == dto.Endpoint);

        if (existing != null)
        {
            existing.P256dh = dto.P256dh;
            existing.Auth = dto.Auth;
            existing.UserId = userId;
        }
        else
        {
            context.DevicePushSubscriptions.Add(new DevicePushSubscription
            {
                UserId = userId,
                Endpoint = dto.Endpoint,
                P256dh = dto.P256dh,
                Auth = dto.Auth,
            });
        }

        await context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("subscriptions")]
    [Authorize]
    public async Task<IActionResult> Unsubscribe([FromBody] PushSubscriptionDto dto)
    {
        var userId = User.GetUserId();
        var sub = await context.DevicePushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == dto.Endpoint && s.UserId == userId);

        if (sub != null)
        {
            context.DevicePushSubscriptions.Remove(sub);
            await context.SaveChangesAsync();
        }

        return Ok();
    }

    [HttpGet("preferences")]
    [Authorize]
    public async Task<IActionResult> GetPreferences()
    {
        var userId = User.GetUserId();
        var prefs = await context.NotificationPreferences.FindAsync(userId);

        // Return defaults if no row exists yet.
        return Ok(prefs != null ? ToDto(prefs) : new NotificationPreferencesDto(true, true, true, true));
    }

    [HttpPut("preferences")]
    [Authorize]
    public async Task<IActionResult> UpdatePreferences([FromBody] NotificationPreferencesDto dto)
    {
        if (!NotificationPreferencesDto.AllowedStreakDangerHours.Contains(dto.StreakDangerHoursBefore))
            return BadRequest("Invalid streak reminder lead time.");

        var userId = User.GetUserId();
        var prefs = await context.NotificationPreferences.FindAsync(userId);

        if (prefs == null)
        {
            prefs = new NotificationPreferences { UserId = userId };
            context.NotificationPreferences.Add(prefs);
        }

        prefs.NewQuestion = dto.NewQuestion;
        prefs.SelectorTurn = dto.SelectorTurn;
        prefs.UserVoted = dto.UserVoted;
        prefs.NewMessage = dto.NewMessage;
        prefs.StreakDanger = dto.StreakDanger;
        prefs.StreakDangerHoursBefore = dto.StreakDangerHoursBefore;

        await context.SaveChangesAsync();
        return Ok(ToDto(prefs));
    }

    private static NotificationPreferencesDto ToDto(NotificationPreferences prefs) =>
        new(
            prefs.NewQuestion,
            prefs.SelectorTurn,
            prefs.UserVoted,
            prefs.NewMessage,
            prefs.StreakDanger,
            prefs.StreakDangerHoursBefore
        );
}
