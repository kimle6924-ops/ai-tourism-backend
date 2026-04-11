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
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest request)
    {
        var result = await _reviewService.CreateAsync(request, GetUserId());
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
    public async Task<IActionResult> GetMyReviews(
        [FromQuery] ResourceType resourceType,
        [FromQuery] Guid resourceId,
        [FromQuery] PaginationRequest request)
    {
        var result = await _reviewService.GetUserReviewsAsync(resourceType, resourceId, GetUserId(), request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("me/history")]
    [Authorize]
    public async Task<IActionResult> GetMyHistory(
        [FromQuery] PaginationRequest request,
        [FromQuery] ResourceType? resourceType)
    {
        var result = await _reviewService.GetMyHistoryAsync(GetUserId(), request, resourceType);
        return StatusCode(result.StatusCode, result);
    }

    // Admin: lấy tất cả reviews (có filter status)
    [HttpGet("all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllReviews(
        [FromQuery] PaginationRequest request,
        [FromQuery] ReviewStatus? status)
    {
        var result = await _reviewService.GetAllReviewsAsync(request, status);
        return StatusCode(result.StatusCode, result);
    }

    // Admin: duyệt review
    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ApproveReview(Guid id)
    {
        var result = await _reviewService.ApproveReviewAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    // Admin: ẩn review
    [HttpPatch("{id:guid}/hide")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> HideReview(Guid id)
    {
        var result = await _reviewService.HideReviewAsync(id);
        return StatusCode(result.StatusCode, result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirst(AppConstants.JwtClaimTypes.UserId)!.Value);

    private string GetRole() =>
        User.FindFirst(ClaimTypes.Role)!.Value;
}
