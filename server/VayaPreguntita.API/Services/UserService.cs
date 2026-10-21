using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Users;
using VayaPreguntita.API.Entities;
using VayaPreguntita.API.Helpers;
using VayaPreguntita.API.Localization;

namespace VayaPreguntita.API.Services;

public class UserService(
    AppDbContext context,
    IPasswordHasher<User> passwordHasher,
    SupabaseStorageService storageService,
    IStreakService streakService,
    IInventoryService inventoryService,
    ICosmeticsService cosmeticsService) : IUserService
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public async Task<UserProfileDto?> GetProfileAsync(int userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null) return null;

        var owned = await cosmeticsService.GetOwnedKeysAsync(userId);
        return new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            FrameColor = user.FrameColor ?? FrameColors.Streak,
            HighestStreak = await streakService.GetHighestStreakAsync(userId),
            HighestStreakEver = user.HighestStreakEver,
            SelectedTitleKey = user.SelectedTitleKey,
            PreferredLanguage = user.PreferredLanguage,
            Title = StreakTiers.DisplayedTitle(user.HighestStreakEver, user.SelectedTitleKey),
            Coins = user.Coins,
            Inventory = await inventoryService.GetAsync(userId),
            UnlockedTitles = StreakTiers
                .UnlockedBy(user.HighestStreakEver)
                .Select(t => new DTOs.Streaks.TitleOptionDto(t.Key, t.Title, t.Material, t.MinDays))
                .ToList(),
            Look = DTOs.Shop.LookDto.From(user),
            ThemeKey = user.ThemeKey,
            OwnedCosmetics = owned,
            FirstJoinedAt = await context
                .GroupMembers.Where(m => m.UserId == userId)
                .MinAsync(m => (DateTime?)m.JoinedAt),
        };
    }

    public async Task<(bool Success, string? Error)> UpdateProfileAsync(int userId, UpdateProfileRequestDto request)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null) return (false, "USER_NOT_FOUND");

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

        // FrameColor: a palette key, "streak", "seniority" or "none"; an empty string also means
        // no frame. A legacy hex from the old picker becomes its key.
        if (request.FrameColor is not null)
        {
            var frame = request.FrameColor.Trim();
            if (frame.Length == 0)
                frame = FrameColors.None;
            if (FrameColors.LegacyHex.TryGetValue(frame, out var key))
                frame = key;
            if (frame != FrameColors.Streak && frame != FrameColors.None && frame != FrameColors.Seniority
                && !FrameColors.PaletteKeys.Contains(frame))
                return (false, "INVALID_FRAME_COLOR");
            user.FrameColor = frame;
        }

        if (request.TitleKey is not null)
        {
            var key = request.TitleKey.Trim();
            if (key == "auto")
                user.SelectedTitleKey = null;
            else if (key == StreakTiers.NoTitle)
                user.SelectedTitleKey = StreakTiers.NoTitle;
            else if (StreakTiers.UnlockedBy(user.HighestStreakEver).Any(t => t.Key == key))
                user.SelectedTitleKey = key;
            else
                return (false, "TITLE_NOT_UNLOCKED");
        }

        // Cosmetic slots: "" takes the item off; anything else must be owned and of that kind.
        foreach (var (requested, kind, apply) in new (string?, Enums.CosmeticKind, Action<string?>)[]
        {
            (request.RingSkinKey, Enums.CosmeticKind.RingSkin, k => user.RingSkinKey = k),
            (request.NameColorKey, Enums.CosmeticKind.NameColor, k => user.NameColorKey = k),
            (request.ThemeKey, Enums.CosmeticKind.Theme, k => user.ThemeKey = k),
        })
        {
            if (requested is null)
                continue;
            var key = requested.Trim();
            if (key.Length == 0)
                apply(null);
            else if (await cosmeticsService.OwnsAsync(userId, key, kind))
                apply(key);
            else
                return (false, "COSMETIC_NOT_OWNED");
        }

        if (request.PreferredLanguage is not null)
        {
            if (!AppLanguages.IsSupported(request.PreferredLanguage))
                return (false, "INVALID_LANGUAGE");
            user.PreferredLanguage = request.PreferredLanguage;
        }

        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, ChangePasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return (false, "PASSWORDS_REQUIRED");

        if (request.NewPassword.Length < 8)
            return (false, "PASSWORD_TOO_SHORT");

        var user = await context.Users.FindAsync(userId);
        if (user is null) return (false, "USER_NOT_FOUND");

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
            return (false, "WRONG_PASSWORD");

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        await context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? AvatarUrl, string? Error)> UploadAvatarAsync(int userId, IFormFile file)
    {
        if (file.Length == 0) return (false, null, "NO_FILE");
        if (file.Length > MaxFileSizeBytes) return (false, null, "FILE_TOO_LARGE");
        if (!AllowedImageTypes.Contains(file.ContentType)) return (false, null, "INVALID_IMAGE_TYPE");

        var user = await context.Users.FindAsync(userId);
        if (user is null) return (false, null, "USER_NOT_FOUND");

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
