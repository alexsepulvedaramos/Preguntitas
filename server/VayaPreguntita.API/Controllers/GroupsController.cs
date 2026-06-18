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

        var hasAccess = await groupsService.IsUserInGroupAsync(userId, groupId);
        if (!hasAccess)
        {
            return Forbid();
        }

        var group = await groupsService.GetGroupAsync(groupId);
        if (group == null)
        {
            return NotFound();
        }

        return Ok(group);
    }

    /// <summary>
    /// PUT /api/groups/{groupId}
    /// Updates the basic information of a group (name, description, etc.).
    /// </summary>
    // [HttpPut("{groupId}")]
    // public async Task<IActionResult> UpdateGroup(int groupId, [FromBody] UpdateGroupRequest request)
    // {
    //     var userId = User.GetUserId();

    //     var hasAccess = await groupsService.IsUserInGroupAsync(userId, groupId);
    //     if (!hasAccess)
    //     {
    //         return Forbid();
    //     }

    //     // TODO: Validate edit permissions (e.g., only the creator can edit)
    //     // TODO: Call the service to update the data

    //     throw new NotImplementedException();
    // }

    /// <summary>
    /// POST /api/groups/join
    /// Allows the authenticated user to join a group using an invitation code.
    /// </summary>
    // [HttpPost("join")]
    // public async Task<IActionResult> JoinGroup([FromBody] JoinGroupRequest request)
    // {
    //     var userId = User.GetUserId();

    //     // TODO: Call the service to find the group by request.InvitationCode
    //     // TODO: Add the user to the group if the code is valid

    //     throw new NotImplementedException();
    // }
}
