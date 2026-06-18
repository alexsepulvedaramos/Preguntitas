using VayaPreguntita.API.DTOs.Groups;

namespace VayaPreguntita.API.Services;

public interface IGroupsService
{
    Task<bool> IsUserInGroupAsync(int userId, int groupId);

    // Retrieves a list of groups for a specific user
    Task<IEnumerable<GroupResponse>> GetUserGroupsAsync(int userId);

    // Creates a new group, generates its invitation code, and returns the details
    Task<GroupResponse> CreateGroupAsync(CreateGroupRequest request, int userId);

    // Retrieves a specific group by its ID
    Task<GroupResponse> GetGroupAsync(int groupId);
}
