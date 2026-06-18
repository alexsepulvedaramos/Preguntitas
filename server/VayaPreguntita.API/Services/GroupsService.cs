using AutoMapper;
using AutoMapper.QueryableExtensions;
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

    // Checks if a user is the admin of the specified group
    public async Task<bool> IsUserAdminAsync(int userId, int groupId)
    {
        return await context.Groups.AnyAsync(g => g.Id == groupId && g.AdminId == userId);
    }

    public async Task<IEnumerable<GroupResponse>> GetUserGroupsAsync(int userId)
    {
        // Fetch groups where the user is part of the Users list
        var groups = await context
            .Groups.Where(g => g.Users.Any(u => u.Id == userId))
            .ProjectTo<GroupResponse>(mapper.ConfigurationProvider)
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
            CreatorId = userId,
            AdminId = userId,
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

    public async Task<GroupResponse?> GetGroupAsync(int groupId)
    {
        var group = await context
            .Groups.Include(g => g.Creator)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        return group == null ? null : mapper.Map<GroupResponse>(group);
    }

    public async Task<GroupResponse?> UpdateGroupAsync(UpdateGroupRequest request, int groupId)
    {
        var group = await context
            .Groups.Include(g => g.Creator)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
            return null;

        group.Name = request.Name;
        group.Description = request.Description;
        group.DailyQuestionTime = request.DailyQuestionTime;

        context.Groups.Update(group);
        await context.SaveChangesAsync();

        return mapper.Map<GroupResponse>(group);
    }

    public async Task<bool> TransferAdminAsync(int groupId, int newAdminId)
    {
        var group = await context
            .Groups.Where(g => g.Id == groupId && g.Users.Any(u => u.Id == newAdminId))
            .FirstOrDefaultAsync();

        if (group == null)
            return false;

        // Reassign the admin role to the new user
        group.AdminId = newAdminId;

        context.Groups.Update(group);
        await context.SaveChangesAsync();

        return true;
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
