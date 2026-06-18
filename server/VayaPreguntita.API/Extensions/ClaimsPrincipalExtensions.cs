using System.Security.Claims;

namespace VayaPreguntita.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    // Extracts the User ID from the JWT claims
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(userIdString, out int userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("User ID not found in token.");
    }
}
