using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VayaPreguntita.API.DTOs.Coins;
using VayaPreguntita.API.DTOs.Users;
using VayaPreguntita.API.Localization;
using VayaPreguntita.API.Services;

namespace VayaPreguntita.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(
    IUserService userService,
    IStreakService streakService,
    ICoinService coinService
) : ControllerBase
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

    // The current user's streak and stats in each of their groups (profile card, rama 19).
    [HttpGet("me/group-stats")]
    public async Task<IActionResult> GetMyGroupStats() =>
        Ok(await streakService.GetUserGroupStatsAsync(CurrentUserId));

    // The coin ledger, newest first, 20 per page (rama 34, §17.2).
    [HttpGet("me/coins/history")]
    public async Task<ActionResult<CoinHistoryPageDto>> GetCoinHistory([FromQuery] int? before) =>
        Ok(await coinService.GetHistoryAsync(CurrentUserId, before));

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequestDto request)
    {
        var (success, error) = await userService.UpdateProfileAsync(CurrentUserId, request);
        if (!success)
        {
            return error switch
            {
                "USERNAME_TAKEN" => Conflict(ServerText.UsernameTaken),
                "EMAIL_TAKEN" => Conflict(ServerText.EmailTaken),
                "USER_NOT_FOUND" => NotFound(ServerText.UserNotFound),
                "INVALID_FRAME_COLOR" => BadRequest(ServerText.InvalidFrameColor),
                "TITLE_NOT_UNLOCKED" => BadRequest(ServerText.TitleNotUnlocked),
                "INVALID_LANGUAGE" => BadRequest(ServerText.InvalidLanguage),
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
                "WRONG_PASSWORD" => BadRequest(ServerText.WrongPassword),
                "PASSWORDS_REQUIRED" => BadRequest(ServerText.PasswordsRequired),
                "PASSWORD_TOO_SHORT" => BadRequest(ServerText.NewPasswordTooShort),
                "USER_NOT_FOUND" => NotFound(ServerText.UserNotFound),
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
            // Validation errors are 400; anything else is a storage failure (503)
            return error switch
            {
                "NO_FILE" => BadRequest(ServerText.NoFileProvided),
                "FILE_TOO_LARGE" => BadRequest(ServerText.FileTooLarge),
                "INVALID_IMAGE_TYPE" => BadRequest(ServerText.InvalidImageType),
                "USER_NOT_FOUND" => NotFound(ServerText.UserNotFound),
                _ => StatusCode(StatusCodes.Status503ServiceUnavailable, error),
            };
        }
        return Ok(new { avatarUrl });
    }
}
