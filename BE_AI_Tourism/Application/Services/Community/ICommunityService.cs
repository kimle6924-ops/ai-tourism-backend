using BE_AI_Tourism.Application.DTOs.Community;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.Services.Community;

public interface ICommunityService
{
    Task<Result<CommunityGroupResponse>> GetPublicGroupAsync();
    Task<Result<PaginationResponse<CommunityPostResponse>>> GetPublicGroupPostsAsync(PaginationRequest request);
    Task<Result<CommunityPostResponse>> CreatePublicPostAsync(CreateCommunityPostRequest request, Guid userId);
    Task<Result<CommunityPostResponse>> GetPostByIdAsync(Guid postId);
    Task<Result<CommunityCommentResponse>> AddCommentAsync(Guid postId, AddCommunityCommentRequest request, Guid userId);
    Task<Result<CommunityPostResponse>> ReactAsync(Guid postId, ReactCommunityPostRequest request, Guid userId);
    Task<Result<CommunityPostUploadSignatureResponse>> GeneratePostUploadSignatureAsync(CommunityPostUploadSignatureRequest request, Guid userId);
    Task<Result<CommunityPostMediaResponse>> FinalizePostMediaAsync(FinalizeCommunityPostMediaRequest request, Guid userId);
}
