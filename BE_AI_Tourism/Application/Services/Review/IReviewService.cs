using BE_AI_Tourism.Application.DTOs.Review;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.Services.Review;

public interface IReviewService
{
    Task<Result<ReviewResponse>> CreateAsync(CreateReviewRequest request, Guid userId);
    Task<Result<ReviewResponse>> UpdateAsync(Guid id, UpdateReviewRequest request, Guid userId);
    Task<Result> DeleteAsync(Guid id, Guid userId, string role);
    Task<Result<ReviewListResponse>> GetByResourceAsync(ResourceType resourceType, Guid resourceId, PaginationRequest request);
    Task<Result<PaginationResponse<ReviewResponse>>> GetUserReviewsAsync(ResourceType resourceType, Guid resourceId, Guid userId, PaginationRequest request);
}
