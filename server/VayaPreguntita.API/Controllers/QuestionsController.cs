using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VayaPreguntita.API.Controllers;

[Authorize]
[ApiController]
[Route("api/groups/{groupId}/questions")]
public class QuestionsController : ControllerBase
{
    // [HttpPost] // POST api/groups/{id}/questions
    // [HttpGet]  // GET api/groups/{id}/questions
}
