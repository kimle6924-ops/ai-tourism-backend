using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using Microsoft.AspNetCore.Mvc;

namespace BE_AI_Tourism.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Check()
    {
        return Ok(Result.Ok<object>(new { Status = AppConstants.HealthCheck.Healthy, Timestamp = DateTime.UtcNow }));
    }
}
