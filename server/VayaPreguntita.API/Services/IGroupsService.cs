using VayaPreguntita.API.DTOs.Groups;

namespace VayaPreguntita.API.Services;

public interface IGroupsService
{
    Task<bool> IsUserInGroupAsync(int userId, int groupId);
    Task<bool> IsUserAdminAsync(int userId, int groupId);

    // Retrieves a list of groups for a specific user
    Task<IEnumerable<GroupResponse>> GetUserGroupsAsync(int userId);

    // Creates a new group, generates its invitation code, and returns the details
    Task<GroupResponse> CreateGroupAsync(CreateGroupRequest request, int userId);

    // Retrieves a specific group by its ID
    Task<GroupResponse?> GetGroupAsync(int groupId);

    // Updates an existing group with new details
    Task<GroupResponse?> UpdateGroupAsync(UpdateGroupRequest request, int groupId);

    // Transfers admin rights to another user
    Task<bool> TransferAdminAsync(int groupId, int newAdminId);

    // Attempts to add a user to a group using an invitation code.
    Task<bool> JoinGroupAsync(int userId, string invitationCode);

    // Retrieves the members of a group, ordered by join date
    Task<IEnumerable<GroupMemberDto>> GetGroupMembersAsync(int groupId, int currentUserId);

    // Removes the user from the group; auto-assigns admin if needed; deletes group if last member
    Task<bool> LeaveGroupAsync(int userId, int groupId);

    // Removes a non-admin member from the group (admin-only action)
    Task<bool> KickMemberAsync(int groupId, int targetUserId);

    // Generates a new invitation code for the group and returns the updated group
    Task<GroupResponse?> RegenerateInviteCodeAsync(int groupId);
}
