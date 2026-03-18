using System.Security.Claims;
using BE_AI_Tourism.Application.DTOs.Media;
using BE_AI_Tourism.Application.Services.Media;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE_AI_Tourism.Controllers;

[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;

    public MediaController(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    [HttpPost("upload-signature")]
    [Authorize(Roles = "Admin,Contributor")]
    public async Task<IActionResult> GenerateSignature([FromBody] UploadSignatureRequest request)
    {
        var result = await _mediaService.GenerateSignatureAsync(request, GetUserId(), GetRole(), GetAdminUnitId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("finalize")]
    [Authorize(Roles = "Admin,Contributor")]
    public async Task<IActionResult> FinalizeUpload([FromBody] FinalizeUploadRequest request)
    {
        var result = await _mediaService.FinalizeUploadAsync(request, GetUserId(), GetRole(), GetAdminUnitId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("by-resource")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByResource([FromQuery] ResourceType resourceType, [FromQuery] Guid resourceId)
    {
        var result = await _mediaService.GetByResourceAsync(resourceType, resourceId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:guid}/set-primary")]
    [Authorize(Roles = "Admin,Contributor")]
    public async Task<IActionResult> SetPrimary(Guid id)
    {
        var result = await _mediaService.SetPrimaryAsync(id, GetUserId(), GetRole(), GetAdminUnitId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("reorder")]
    [Authorize(Roles = "Admin,Contributor")]
    public async Task<IActionResult> Reorder([FromBody] ReorderMediaRequest request)
    {
        var result = await _mediaService.ReorderAsync(request, GetUserId(), GetRole(), GetAdminUnitId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Contributor")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediaService.DeleteAsync(id, GetUserId(), GetRole(), GetAdminUnitId());
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
