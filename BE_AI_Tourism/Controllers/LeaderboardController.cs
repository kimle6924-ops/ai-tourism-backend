using BE_AI_Tourism.Application.Services.Leaderboard;
using BE_AI_Tourism.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE_AI_Tourism.Controllers;

[ApiController]
[Route("api/leaderboard")]
[AllowAnonymous]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUserLeaderboard([FromQuery] PaginationRequest request)
    {
        var result = await _leaderboardService.GetUserLeaderboardAsync(request);
        return StatusCode(result.StatusCode, result);
    }
}
