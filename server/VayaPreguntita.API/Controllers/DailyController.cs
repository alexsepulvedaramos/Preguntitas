using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VayaPreguntita.API.Controllers;

[Authorize]
[ApiController]
[Route("api/daily")]
public class DailyController : ControllerBase
{
    // [HttpGet("current")] // GET api/groups/{id}/daily/current
    // [HttpPost("vote")]   // POST api/groups/{id}/daily/vote
}
