using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Services;

namespace VayaPreguntita.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    AppDbContext context,
    JwtTokenService tokenService,
    IPasswordHasher<User> passwordHasher
) : ControllerBase
{
    private readonly AppDbContext _context = context;
    private readonly JwtTokenService _tokenService = tokenService;
    private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto request)
    {
        if (!IsValidRegistration(request))
            return BadRequest("Username, email and password are required.");

        var normalizedUsername = request.Username.Trim();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var usernameExists = await _context.Users.AnyAsync(user =>
            user.Username.ToLower() == normalizedUsername.ToLower()
        );

        if (usernameExists)
            return Conflict("Username is already in use.");

        var emailExists = await _context.Users.AnyAsync(user =>
            user.Email.ToLower() == normalizedEmail
        );

        if (emailExists)
            return Conflict("Email is already in use.");

        var user = new User { Username = normalizedUsername, Email = normalizedEmail };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        var refreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = JwtTokenService.HashRefreshToken(refreshToken);
        user.RefreshTokenExpiry = _tokenService.GetRefreshTokenExpiry();

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var accessToken = _tokenService.GenerateAccessToken(user, out var accessTokenExpiresAt);

        return StatusCode(
            StatusCodes.Status201Created,
            BuildResponse(user, accessToken, accessTokenExpiresAt, refreshToken)
        );
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto request)
    {
        if (!IsValidLogin(request))
            return BadRequest("Identifier and password are required.");

        var normalizedIdentifier = request.Identifier.Trim();

        var user = await _context.Users.SingleOrDefaultAsync(user =>
            user.Username.ToLower() == normalizedIdentifier.ToLower()
            || user.Email.ToLower() == normalizedIdentifier.ToLower()
        );

        if (user == null)
            return Unauthorized("Invalid credentials.");

        var verification = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );
        if (verification == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid credentials.");

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        }

        var refreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = JwtTokenService.HashRefreshToken(refreshToken);
        user.RefreshTokenExpiry = _tokenService.GetRefreshTokenExpiry();

        await _context.SaveChangesAsync();

        var accessToken = _tokenService.GenerateAccessToken(user, out var accessTokenExpiresAt);

        return Ok(BuildResponse(user, accessToken, accessTokenExpiresAt, refreshToken));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshTokenRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest("Refresh token is required.");

        var hashedRefreshToken = JwtTokenService.HashRefreshToken(request.RefreshToken.Trim());

        var user = await _context.Users.SingleOrDefaultAsync(user =>
            user.RefreshToken == hashedRefreshToken
            && user.RefreshTokenExpiry != null
            && user.RefreshTokenExpiry > DateTime.UtcNow
        );

        if (user == null)
            return Unauthorized("Invalid or expired refresh token.");

        var newRefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshToken = JwtTokenService.HashRefreshToken(newRefreshToken);
        user.RefreshTokenExpiry = _tokenService.GetRefreshTokenExpiry();

        await _context.SaveChangesAsync();

        var accessToken = _tokenService.GenerateAccessToken(user, out var accessTokenExpiresAt);

        return Ok(BuildResponse(user, accessToken, accessTokenExpiresAt, newRefreshToken));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest("Refresh token is required.");

        var hashedRefreshToken = JwtTokenService.HashRefreshToken(request.RefreshToken.Trim());

        var user = await _context.Users.SingleOrDefaultAsync(user =>
            user.RefreshToken == hashedRefreshToken
        );

        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpGet("check-username")]
    public async Task<IActionResult> CheckUsername([FromQuery] string username)
    {
        var exists = await _context.Users.AnyAsync(u => u.Username == username);
        return Ok(new { exists });
    }

    [HttpGet("check-email")]
    public async Task<IActionResult> CheckEmail([FromQuery] string email)
    {
        var exists = await _context.Users.AnyAsync(u => u.Email == email);
        return Ok(new { exists });
    }

    private static bool IsValidRegistration(RegisterRequestDto request)
    {
        return !string.IsNullOrWhiteSpace(request.Username)
            && !string.IsNullOrWhiteSpace(request.Email)
            && !string.IsNullOrWhiteSpace(request.Password)
            && request.Password.Length >= 8;
    }

    private static bool IsValidLogin(LoginRequestDto request)
    {
        return !string.IsNullOrWhiteSpace(request.Identifier)
            && !string.IsNullOrWhiteSpace(request.Password);
    }

    private static AuthResponseDto BuildResponse(
        User user,
        string accessToken,
        DateTime accessTokenExpiresAt,
        string refreshToken
    )
    {
        return new AuthResponseDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            AvatarUrl = null,
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessTokenExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = user.RefreshTokenExpiry ?? DateTime.UtcNow,
        };
    }
}
