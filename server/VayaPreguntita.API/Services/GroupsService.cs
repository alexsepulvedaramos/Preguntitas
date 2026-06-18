using AutoMapper;
using Microsoft.EntityFrameworkCore;
using VayaPreguntita.API.Data;
using VayaPreguntita.API.DTOs.Groups;
using VayaPreguntita.API.Entities;

namespace VayaPreguntita.API.Services;

public class GroupsService(AppDbContext context, IMapper mapper) : IGroupsService
{
    // Checks if a user is part of a specific group
    public async Task<bool> IsUserInGroupAsync(int userId, int groupId)
    {
        return await context.Groups.AnyAsync(g =>
            g.Id == groupId && g.Users.Any(u => u.Id == userId)
        );
    }

    public async Task<IEnumerable<GroupResponse>> GetUserGroupsAsync(int userId)
    {
        // Fetch groups where the user is part of the Users list
        var groups = await context
            .Groups.Where(g => g.Users.Any(u => u.Id == userId))
            .ToListAsync();

        // Map the list of Group entities directly to a list of GroupResponse DTOs
        return mapper.Map<IEnumerable<GroupResponse>>(groups);
    }

    public async Task<GroupResponse> CreateGroupAsync(CreateGroupRequest request, int userId)
    {
        // 1. Generate a unique short invitation code
        var invitationCode = GenerateRandomCode(6);

        // 2. Fetch the user from the database to attach them to the new group
        var user =
            await context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        // 3. Create the new Group entity
        var newGroup = new Group
        {
            Name = request.Name,
            Description = request.Description,
            DailyQuestionTime = request.DailyQuestionTime,
            InvitationCode = invitationCode,
            Users = [user],
        };

        context.Groups.Add(newGroup);
        await context.SaveChangesAsync();

        // 4. Map and return the response DTO
        return mapper.Map<GroupResponse>(newGroup);
    }

    public async Task<GroupResponse> GetGroupAsync(int groupId)
    {
        // Fetch the specific group where the user is part of the Users list
        var group =
            await context.Groups.FirstOrDefaultAsync(g => g.Id == groupId)
            ?? throw new KeyNotFoundException("Group not found.");

        // Map the Group entity to a GroupResponse DTO
        return mapper.Map<GroupResponse>(group);
    }

    // Helper method to generate a short alphanumeric string
    private static string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(
            Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray()
        );
    }
}
