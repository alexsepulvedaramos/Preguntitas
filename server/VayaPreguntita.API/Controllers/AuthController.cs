using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Auth;
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

    private const int MaxSessionsPerUser = 5;

    [HttpPost("register")]
    [EnableRateLimiting("AuthLimiter")]
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

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var (refreshToken, refreshTokenExpiresAt) = await CreateRefreshTokenAsync(user.Id);
        var accessToken = _tokenService.GenerateAccessToken(user, out var accessTokenExpiresAt);

        return StatusCode(
            StatusCodes.Status201Created,
            BuildResponse(user, accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt)
        );
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthLimiter")]
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
            await _context.SaveChangesAsync();
        }

        var (refreshToken, refreshTokenExpiresAt) = await CreateRefreshTokenAsync(user.Id);
        var accessToken = _tokenService.GenerateAccessToken(user, out var accessTokenExpiresAt);

        return Ok(BuildResponse(user, accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshTokenRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest("Refresh token is required.");

        var hashedToken = JwtTokenService.HashRefreshToken(request.RefreshToken.Trim());

        var existingToken = await _context.UserRefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t =>
                t.TokenHash == hashedToken
                && t.ExpiresAt > DateTime.UtcNow);

        if (existingToken == null)
            return Unauthorized("Invalid or expired refresh token.");

        var user = existingToken.User;

        // Rotate the token in-place: update hash and expiry on the same row
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        existingToken.TokenHash = JwtTokenService.HashRefreshToken(newRefreshToken);
        existingToken.ExpiresAt = _tokenService.GetRefreshTokenExpiry();

        await _context.SaveChangesAsync();

        var accessToken = _tokenService.GenerateAccessToken(user, out var accessTokenExpiresAt);

        return Ok(BuildResponse(user, accessToken, accessTokenExpiresAt, newRefreshToken, existingToken.ExpiresAt));
    }

    [HttpPost("logout")]
    [EnableRateLimiting("AuthLimiter")]
    public async Task<IActionResult> Logout(RefreshTokenRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest("Refresh token is required.");

        var hashedToken = JwtTokenService.HashRefreshToken(request.RefreshToken.Trim());

        var token = await _context.UserRefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == hashedToken);

        if (token != null)
        {
            _context.UserRefreshTokens.Remove(token);
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

    // Creates a new refresh token for the user, cleaning up expired tokens and
    // evicting the least recently used session when the per-user limit is reached.
    private async Task<(string Token, DateTime ExpiresAt)> CreateRefreshTokenAsync(int userId)
    {
        // Remove expired tokens
        var expired = await _context.UserRefreshTokens
            .Where(t => t.UserId == userId && t.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();
        _context.UserRefreshTokens.RemoveRange(expired);

        // Evict the least recently used session if at the limit. Expiry slides forward on every
        // refresh, so the earliest ExpiresAt is the session that has been idle the longest —
        // a device the user opens daily is never kicked out by an abandoned browser.
        var active = await _context.UserRefreshTokens
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.ExpiresAt)
            .ToListAsync();

        if (active.Count >= MaxSessionsPerUser)
            _context.UserRefreshTokens.RemoveRange(active.Take(active.Count - MaxSessionsPerUser + 1));

        var rawToken = _tokenService.GenerateRefreshToken();
        var expiresAt = _tokenService.GetRefreshTokenExpiry();

        _context.UserRefreshTokens.Add(new UserRefreshToken
        {
            UserId = userId,
            TokenHash = JwtTokenService.HashRefreshToken(rawToken),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync();

        return (rawToken, expiresAt);
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
        string refreshToken,
        DateTime refreshTokenExpiresAt
    )
    {
        return new AuthResponseDto
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessTokenExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
        };
    }
}
