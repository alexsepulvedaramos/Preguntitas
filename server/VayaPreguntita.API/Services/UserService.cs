using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Users;
using VayaPreguntita.API.Entities;

namespace VayaPreguntita.API.Services;

public class UserService(
    AppDbContext context,
    IPasswordHasher<User> passwordHasher,
    SupabaseStorageService storageService) : IUserService
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public async Task<UserProfileDto?> GetProfileAsync(int userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null) return null;

        return new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            FrameColor = user.FrameColor,
        };
    }

    public async Task<(bool Success, string? Error)> UpdateProfileAsync(int userId, UpdateProfileRequestDto request)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null) return (false, "User not found.");

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            var normalizedUsername = request.Username.Trim();
            if (normalizedUsername != user.Username)
            {
                var taken = await context.Users.AnyAsync(u =>
                    u.Id != userId && u.Username.ToLower() == normalizedUsername.ToLower());
                if (taken) return (false, "USERNAME_TAKEN");
                user.Username = normalizedUsername;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            if (normalizedEmail != user.Email)
            {
                var taken = await context.Users.AnyAsync(u =>
                    u.Id != userId && u.Email.ToLower() == normalizedEmail);
                if (taken) return (false, "EMAIL_TAKEN");
                user.Email = normalizedEmail;
            }
        }

        // AvatarUrl may be a DiceBear URL or null (reset)
        if (request.AvatarUrl is not null)
            user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl;

        // FrameColor: explicit null resets to no frame
        if (request.FrameColor is not null)
            user.FrameColor = string.IsNullOrWhiteSpace(request.FrameColor) ? null : request.FrameColor;

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, ChangePasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return (false, "Both current and new password are required.");

        if (request.NewPassword.Length < 8)
            return (false, "New password must be at least 8 characters.");

        var user = await context.Users.FindAsync(userId);
        if (user is null) return (false, "User not found.");

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
            return (false, "WRONG_PASSWORD");

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? AvatarUrl, string? Error)> UploadAvatarAsync(int userId, IFormFile file)
    {
        if (file.Length == 0) return (false, null, "No file provided.");
        if (file.Length > MaxFileSizeBytes) return (false, null, "File exceeds 5 MB limit.");
        if (!AllowedImageTypes.Contains(file.ContentType)) return (false, null, "Invalid image type.");

        var user = await context.Users.FindAsync(userId);
        if (user is null) return (false, null, "User not found.");

        try
        {
            using var stream = file.OpenReadStream();
            var publicUrl = await storageService.UploadAvatarAsync(userId, stream, file.ContentType);

            user.AvatarUrl = publicUrl;
            await context.SaveChangesAsync();

            return (true, publicUrl, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }
}
