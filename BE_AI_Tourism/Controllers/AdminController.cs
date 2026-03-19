using BE_AI_Tourism.Application.Services.Admin;
using BE_AI_Tourism.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE_AI_Tourism.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;
    private readonly IAdminStatsService _adminStatsService;

    public AdminController(IAdminUserService adminUserService, IAdminStatsService adminStatsService)
    {
        _adminUserService = adminUserService;
        _adminStatsService = adminStatsService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] PaginationRequest request)
    {
        var result = await _adminUserService.GetUsersAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("users/{id}/lock")]
    public async Task<IActionResult> LockUser(Guid id)
    {
        var result = await _adminUserService.LockUserAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("users/{id}/unlock")]
    public async Task<IActionResult> UnlockUser(Guid id)
    {
        var result = await _adminUserService.UnlockUserAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("users/{id}/approve")]
    public async Task<IActionResult> ApproveUser(Guid id)
    {
        var result = await _adminUserService.ApproveUserAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("stats/overview")]
    public async Task<IActionResult> GetStatsOverview()
    {
        var result = await _adminStatsService.GetOverviewAsync();
        return StatusCode(result.StatusCode, result);
    }
}
