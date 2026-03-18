using BE_AI_Tourism.Application.DTOs.Administrative;
using BE_AI_Tourism.Application.Services.Administrative;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE_AI_Tourism.Controllers;

[ApiController]
[Route("api/administrative-units")]
public class AdministrativeUnitController : ControllerBase
{
    private readonly IAdministrativeUnitService _service;

    public AdministrativeUnitController(IAdministrativeUnitService service)
    {
        _service = service;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPaged([FromQuery] PaginationRequest request)
    {
        var result = await _service.GetPagedAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("by-level/{level}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByLevel(AdministrativeLevel level)
    {
        var result = await _service.GetByLevelAsync(level);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("{id:guid}/children")]
    [AllowAnonymous]
    public async Task<IActionResult> GetChildren(Guid id)
    {
        var result = await _service.GetChildrenAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateAdministrativeUnitRequest request)
    {
        var result = await _service.CreateAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAdministrativeUnitRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);
        return StatusCode(result.StatusCode, result);
    }
}
