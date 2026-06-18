using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VayaPreguntita.API.Data;

namespace VayaPreguntita.API.Controllers;

[Authorize]
[ApiController]
[Route("api/groups")]
public class GroupsController(AppDbContext context, IMapper mapper) : ControllerBase
{
    private readonly AppDbContext _context = context;
    private readonly IMapper _mapper = mapper;

    // [HttpGet] // GET api/groups
    // public async Task<IActionResult> GetGroups()
    // {
    //     var groups = await _context.Groups.ToListAsync();
    //     var groupsDto = _mapper.Map<List<GroupDto>>(groups);
    //     return Ok(groupsDto);
    // }

    // [HttpPost] // POST api/groups
    // [HttpPatch("{id}")] // PATCH api/groups/{id}
    // [HttpPost("join")] // POST api/groups/join
}
