using System.Security.Claims;
using BE_AI_Tourism.Application.DTOs.Place;
using BE_AI_Tourism.Application.Services.Place;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE_AI_Tourism.Controllers;

[ApiController]
[Route("api/places")]
public class PlaceController : ControllerBase
{
    private readonly IPlaceService _placeService;

    public PlaceController(IPlaceService placeService)
    {
        _placeService = placeService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetApproved([FromQuery] PaginationRequest request)
    {
        var result = await _placeService.GetApprovedPagedAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("all")]
    [Authorize(Roles = "Admin,Contributor")]
    public async Task<IActionResult> GetAll([FromQuery] PlaceAdminQueryRequest request)
    {
        var result = await _placeService.GetAllPagedAsync(request, GetUserId(), GetRole(), GetContributorType(), GetAdminUnitId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _placeService.GetByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Contributor")]
    public async Task<IActionResult> Create([FromBody] CreatePlaceRequest request)
    {
        var result = await _placeService.CreateAsync(request, GetUserId(), GetRole(), GetContributorType(), GetAdminUnitId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Contributor")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePlaceRequest request)
    {
        var result = await _placeService.UpdateAsync(id, request, GetUserId(), GetRole(), GetContributorType(), GetAdminUnitId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Contributor")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _placeService.DeleteAsync(id, GetUserId(), GetRole(), GetContributorType(), GetAdminUnitId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("seed")]
    [AllowAnonymous]
    public async Task<IActionResult> Seed()
    {
        var result = await _placeService.SeedAsync();
        return StatusCode(result.StatusCode, result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(AppConstants.JwtClaimTypes.UserId)!.Value);

    private string GetRole() =>
        User.FindFirst(ClaimTypes.Role)!.Value;

    private ContributorType? GetContributorType()
    {
        var claim = User.FindFirst(AppConstants.JwtClaimTypes.ContributorType)?.Value;
        return Enum.TryParse<ContributorType>(claim, out var ct) ? ct : null;
    }

    private Guid? GetAdminUnitId()
    {
        var claim = User.FindFirst(AppConstants.JwtClaimTypes.AdministrativeUnitId)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
