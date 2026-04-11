using BE_AI_Tourism.Application.DTOs.Community;
using BE_AI_Tourism.Application.Services.Community;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE_AI_Tourism.Controllers;

[ApiController]
[Route("api/community")]
public class CommunityController : ControllerBase
{
    private readonly ICommunityService _communityService;

    public CommunityController(ICommunityService communityService)
    {
        _communityService = communityService;
    }

    [HttpGet("group/public")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicGroup()
    {
        var result = await _communityService.GetPublicGroupAsync();
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("group/public/posts")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicGroupPosts([FromQuery] PaginationRequest request)
    {
        var result = await _communityService.GetPublicGroupPostsAsync(request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("group/public/posts")]
    [Authorize]
    public async Task<IActionResult> CreatePublicPost([FromBody] CreateCommunityPostRequest request)
    {
        var result = await _communityService.CreatePublicPostAsync(request, GetUserId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("posts/{postId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPostById(Guid postId)
    {
        var result = await _communityService.GetPostByIdAsync(postId);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("posts/{postId:guid}/comments")]
    [Authorize]
    public async Task<IActionResult> AddComment(Guid postId, [FromBody] AddCommunityCommentRequest request)
    {
        var result = await _communityService.AddCommentAsync(postId, request, GetUserId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("posts/{postId:guid}/reactions")]
    [Authorize]
    public async Task<IActionResult> React(Guid postId, [FromBody] ReactCommunityPostRequest request)
    {
        var result = await _communityService.ReactAsync(postId, request, GetUserId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("posts/upload-signature")]
    [Authorize]
    public async Task<IActionResult> UploadSignature([FromBody] CommunityPostUploadSignatureRequest request)
    {
        var result = await _communityService.GeneratePostUploadSignatureAsync(request, GetUserId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("posts/finalize-media")]
    [Authorize]
    public async Task<IActionResult> FinalizeMedia([FromBody] FinalizeCommunityPostMediaRequest request)
    {
        var result = await _communityService.FinalizePostMediaAsync(request, GetUserId());
        return StatusCode(result.StatusCode, result);
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirst(AppConstants.JwtClaimTypes.UserId)!.Value);
}
