using System.Security.Claims;
using BE_AI_Tourism.Application.DTOs.Review;
using BE_AI_Tourism.Application.Services.Review;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE_AI_Tourism.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateOrUpdate([FromBody] CreateReviewRequest request)
    {
        var result = await _reviewService.CreateOrUpdateAsync(request, GetUserId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReviewRequest request)
    {
        var result = await _reviewService.UpdateAsync(id, request, GetUserId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _reviewService.DeleteAsync(id, GetUserId(), GetRole());
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetByResource(
        [FromQuery] ResourceType resourceType,
        [FromQuery] Guid resourceId,
        [FromQuery] PaginationRequest request)
    {
        var result = await _reviewService.GetByResourceAsync(resourceType, resourceId, request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMyReview(
        [FromQuery] ResourceType resourceType,
        [FromQuery] Guid resourceId)
    {
        var result = await _reviewService.GetUserReviewAsync(resourceType, resourceId, GetUserId());
        return StatusCode(result.StatusCode, result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(AppConstants.JwtClaimTypes.UserId)!.Value);

    private string GetRole() =>
        User.FindFirst(ClaimTypes.Role)!.Value;
}
