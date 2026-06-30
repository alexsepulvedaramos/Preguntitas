using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VayaPreguntita.API.DTOs.Users;
using VayaPreguntita.API.Services;

namespace VayaPreguntita.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserService userService) : ControllerBase
{
    private int CurrentUserId => int.Parse(
        User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var profile = await userService.GetProfileAsync(CurrentUserId);
        if (profile is null) return NotFound();
        return Ok(profile);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequestDto request)
    {
        var (success, error) = await userService.UpdateProfileAsync(CurrentUserId, request);
        if (!success)
        {
            return error switch
            {
                "USERNAME_TAKEN" => Conflict("Username is already in use."),
                "EMAIL_TAKEN" => Conflict("Email is already in use."),
                _ => BadRequest(error),
            };
        }
        var profile = await userService.GetProfileAsync(CurrentUserId);
        return Ok(profile);
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequestDto request)
    {
        var (success, error) = await userService.ChangePasswordAsync(CurrentUserId, request);
        if (!success)
        {
            return error switch
            {
                "WRONG_PASSWORD" => BadRequest("Current password is incorrect."),
                _ => BadRequest(error),
            };
        }
        return NoContent();
    }

    [HttpPost("me/avatar")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var (success, avatarUrl, error) = await userService.UploadAvatarAsync(CurrentUserId, file);
        if (!success)
        {
            // Distinguish validation errors (400) from storage failures (503)
            var isValidationError = error is "No file provided." or "File exceeds 5 MB limit." or "Invalid image type.";
            return isValidationError
                ? BadRequest(error)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, error);
        }
        return Ok(new { avatarUrl });
    }
}
