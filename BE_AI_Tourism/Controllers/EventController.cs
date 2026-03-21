using System.Security.Claims;
using BE_AI_Tourism.Application.DTOs.Event;
using BE_AI_Tourism.Application.Services.Event;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE_AI_Tourism.Controllers;

[ApiController]
[Route("api/events")]
public class EventController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetApproved([FromQuery] PaginationRequest request)
    {
        var result = await _eventService.GetApprovedPagedAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin,Contributor")]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request)
    {
        var result = await _eventService.GetAllPagedAsync(request, GetRole(), GetAdminUnitId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _eventService.GetByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Contributor")]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        var result = await _eventService.CreateAsync(request, GetUserId(), GetRole(), GetAdminUnitId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Contributor")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request)
    {
        var result = await _eventService.UpdateAsync(id, request, GetUserId(), GetRole(), GetAdminUnitId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Contributor")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _eventService.DeleteAsync(id, GetUserId(), GetRole(), GetAdminUnitId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("seed")]
    [AllowAnonymous]
    public async Task<IActionResult> Seed()
    {
        var result = await _eventService.SeedAsync();
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
