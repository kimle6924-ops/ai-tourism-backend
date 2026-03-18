using System.Security.Claims;
using BE_AI_Tourism.Application.DTOs.Moderation;
using BE_AI_Tourism.Application.Services.Moderation;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE_AI_Tourism.Controllers;

[ApiController]
[Route("api/moderation")]
[Authorize(Roles = "Admin,Contributor")]
public class ModerationController : ControllerBase
{
    private readonly IModerationService _moderationService;

    public ModerationController(IModerationService moderationService)
    {
        _moderationService = moderationService;
    }

    [HttpPatch("{resourceType}/{id:guid}/approve")]
    public async Task<IActionResult> Approve(ResourceType resourceType, Guid id, [FromBody] ModerationActionRequest request)
    {
        var result = await _moderationService.ApproveAsync(resourceType, id, request, GetUserId(), GetRole(), GetAdminUnitId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{resourceType}/{id:guid}/reject")]
    public async Task<IActionResult> Reject(ResourceType resourceType, Guid id, [FromBody] ModerationActionRequest request)
    {
        var result = await _moderationService.RejectAsync(resourceType, id, request, GetUserId(), GetRole(), GetAdminUnitId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{resourceType}/{id:guid}/logs")]
    public async Task<IActionResult> GetLogs(ResourceType resourceType, Guid id)
    {
        var result = await _moderationService.GetLogsAsync(resourceType, id);
        return StatusCode(result.StatusCode, result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(AppConstants.JwtClaimTypes.UserId)!.Value);

    private string GetRole() =>
        User.FindFirst(ClaimTypes.Role)!.Value;

    private Guid? GetAdminUnitId()
    {
        var claim = User.FindFirst(AppConstants.JwtClaimTypes.AdministrativeUnitId)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
