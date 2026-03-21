using BE_AI_Tourism.Application.DTOs.Review;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;
using MapsterMapper;

namespace BE_AI_Tourism.Application.Services.Review;

public class ReviewService : IReviewService
{
    private readonly IRepository<Domain.Entities.Review> _reviewRepository;
    private readonly IRepository<Domain.Entities.Place> _placeRepository;
    private readonly IRepository<Domain.Entities.Event> _eventRepository;
    private readonly IMapper _mapper;

    public ReviewService(
        IRepository<Domain.Entities.Review> reviewRepository,
        IRepository<Domain.Entities.Place> placeRepository,
        IRepository<Domain.Entities.Event> eventRepository,
        IMapper mapper)
    {
        _reviewRepository = reviewRepository;
        _placeRepository = placeRepository;
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<Result<ReviewResponse>> CreateAsync(CreateReviewRequest request, Guid userId)
    {
        // Verify resource exists and is approved
        if (!await IsResourceApproved(request.ResourceType, request.ResourceId))
            return Result.Fail<ReviewResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        var entity = new Domain.Entities.Review
        {
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment,
            Status = ReviewStatus.Active
        };

        await _reviewRepository.AddAsync(entity);
        return Result.Ok(_mapper.Map<ReviewResponse>(entity), StatusCodes.Status201Created);
    }

    public async Task<Result<ReviewResponse>> UpdateAsync(Guid id, UpdateReviewRequest request, Guid userId)
    {
        var review = await _reviewRepository.GetByIdAsync(id);
        if (review == null)
            return Result.Fail<ReviewResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        if (review.UserId != userId)
            return Result.Fail<ReviewResponse>(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

        review.Rating = request.Rating;
        review.Comment = request.Comment;
        await _reviewRepository.UpdateAsync(review);
        return Result.Ok(_mapper.Map<ReviewResponse>(review));
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, string role)
    {
        var review = await _reviewRepository.GetByIdAsync(id);
        if (review == null)
            return Result.Fail(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        // Owner or Admin can delete
        if (review.UserId != userId && role != UserRole.Admin.ToString())
            return Result.Fail(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

        await _reviewRepository.DeleteAsync(id);
        return Result.Ok("Review deleted successfully");
    }

    public async Task<Result<ReviewListResponse>> GetByResourceAsync(ResourceType resourceType, Guid resourceId, PaginationRequest request)
    {
        var all = await _reviewRepository.FindAsync(
            r => r.ResourceType == resourceType && r.ResourceId == resourceId && r.Status == ReviewStatus.Active);
        var ordered = all.OrderByDescending(r => r.CreatedAt).ToList();
        var items = ordered.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();
        var responses = items.Select(r => _mapper.Map<ReviewResponse>(r)).ToList();

        var totalReviews = ordered.Count;
        var averageRating = totalReviews > 0 ? Math.Round(ordered.Average(r => r.Rating), 1) : 0;

        return Result.Ok(new ReviewListResponse
        {
            AverageRating = averageRating,
            TotalReviews = totalReviews,
            Reviews = PaginationResponse<ReviewResponse>.Create(
                responses, totalReviews, request.PageNumber, request.PageSize)
        });
    }

    public async Task<Result<PaginationResponse<ReviewResponse>>> GetUserReviewsAsync(ResourceType resourceType, Guid resourceId, Guid userId, PaginationRequest request)
    {
        var all = await _reviewRepository.FindAsync(
            r => r.ResourceType == resourceType && r.ResourceId == resourceId && r.UserId == userId && r.Status == ReviewStatus.Active);
        var ordered = all.OrderByDescending(r => r.CreatedAt).ToList();
        var items = ordered.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();
        var responses = items.Select(r => _mapper.Map<ReviewResponse>(r)).ToList();

        return Result.Ok(PaginationResponse<ReviewResponse>.Create(
            responses, ordered.Count, request.PageNumber, request.PageSize));
    }

    private async Task<bool> IsResourceApproved(ResourceType resourceType, Guid resourceId)
    {
        if (resourceType == ResourceType.Place)
        {
            var place = await _placeRepository.GetByIdAsync(resourceId);
            return place is { ModerationStatus: ModerationStatus.Approved };
        }

        var evt = await _eventRepository.GetByIdAsync(resourceId);
        return evt is { ModerationStatus: ModerationStatus.Approved };
    }
}
