using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VayaPreguntita.API.DTOs.Groups;
using VayaPreguntita.API.Extensions;
using VayaPreguntita.API.Services;

// Usings for your DTOs (e.g., VayaPreguntita.API.DTOs.Groups)

namespace VayaPreguntita.API.Controllers;

[Authorize]
[ApiController]
[Route("api/groups")]
public class GroupsController(IGroupsService groupsService) : ControllerBase
{
    /// <summary>
    /// GET /api/groups
    /// Lists the groups to which the authenticated user belongs.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserGroups()
    {
        var userId = User.GetUserId();
        var groups = await groupsService.GetUserGroupsAsync(userId);

        return Ok(groups);
    }

    /// <summary>
    /// POST /api/groups
    /// Creates a new group and assigns the creator user as a member/administrator.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        var userId = User.GetUserId();
        var newGroup = await groupsService.CreateGroupAsync(request, userId);

        return CreatedAtAction(nameof(GetGroup), new { groupId = newGroup.Id }, newGroup);
    }

    /// <summary>
    /// GET /api/groups/{groupId}
    /// Retrieves the details of a specific group.
    /// </summary>
    [HttpGet("{groupId}")]
    public async Task<IActionResult> GetGroup(int groupId)
    {
        var userId = User.GetUserId();

        var group = await groupsService.GetGroupAsync(groupId);
        if (group == null)
            return NotFound();

        var hasAccess = await groupsService.IsUserInGroupAsync(userId, groupId);
        if (!hasAccess)
            return Forbid();

        return Ok(group);
    }

    /// <summary>
    /// PUT /api/groups/{groupId}
    /// Updates the basic information of a group (name, description, etc.).
    /// </summary>
    [HttpPut("{groupId}")]
    public async Task<IActionResult> UpdateGroup(int groupId, [FromBody] UpdateGroupRequest request)
    {
        var userId = User.GetUserId();

        var isAdmin = await groupsService.IsUserAdminAsync(userId, groupId);
        if (!isAdmin)
            return Forbid();

        var updatedGroup = await groupsService.UpdateGroupAsync(request, groupId);

        if (updatedGroup == null)
        {
            return NotFound();
        }

        // Return 200 OK with the updated group to sync the frontend state immediately
        return Ok(updatedGroup);
    }

    /// <summary>
    /// GET /api/groups/{groupId}/members
    /// Lists the members of a group, including who the admin is.
    /// </summary>
    [HttpGet("{groupId}/members")]
    public async Task<IActionResult> GetGroupMembers(int groupId)
    {
        var userId = User.GetUserId();

        var group = await groupsService.GetGroupAsync(groupId);
        if (group == null)
            return NotFound();

        var hasAccess = await groupsService.IsUserInGroupAsync(userId, groupId);
        if (!hasAccess)
            return Forbid();

        var members = await groupsService.GetGroupMembersAsync(groupId);
        return Ok(members);
    }

    /// <summary>
    /// POST /api/groups/join
    /// Allows the authenticated user to join a group using an invitation code.
    /// </summary>
    [HttpPost("join")]
    public async Task<IActionResult> JoinGroup([FromBody] JoinGroupRequest request)
    {
        var userId = User.GetUserId();

        var hasJoined = await groupsService.JoinGroupAsync(userId, request.InvitationCode);
        if (!hasJoined)
            return BadRequest(new { Message = "Invalid invitation code or group not found." });

        return Ok();
    }

    /// <summary>
    /// PUT /api/groups/{id}/admin
    /// Transfers the administrator role to another user in the group.
    /// </summary>
    [HttpPut("{id}/admin")]
    public async Task<IActionResult> TransferAdmin(int id, [FromBody] TransferAdminRequest request)
    {
        var userId = User.GetUserId();

        var isAdmin = await groupsService.IsUserAdminAsync(userId, id);
        if (!isAdmin)
            return Forbid();

        var success = await groupsService.TransferAdminAsync(id, request.NewAdminId);

        // Return 400 Bad Request indicating the specific business rule violation
        if (!success)
            return BadRequest("Target user must be an active member of the group to become admin.");

        // Return 204 No Content as the action succeeded and there's no data to return
        return NoContent();
    }
}
