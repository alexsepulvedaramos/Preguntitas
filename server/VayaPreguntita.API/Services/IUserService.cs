using VayaPreguntita.API.DTOs.Users;

namespace VayaPreguntita.API.Services;

public interface IUserService
{
    Task<UserProfileDto?> GetProfileAsync(int userId);
    Task<(bool Success, string? Error)> UpdateProfileAsync(int userId, UpdateProfileRequestDto request);
    Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, ChangePasswordRequestDto request);
    Task<(bool Success, string? AvatarUrl, string? Error)> UploadAvatarAsync(int userId, IFormFile file);
}
