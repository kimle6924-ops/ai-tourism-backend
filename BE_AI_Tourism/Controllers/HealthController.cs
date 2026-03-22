using Microsoft.AspNetCore.Mvc;

namespace BE_AI_Tourism.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { status = "alive", timestamp = DateTime.UtcNow });
}
