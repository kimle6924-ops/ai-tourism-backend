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
    private readonly IRepository<Domain.Entities.MediaAsset> _mediaRepository;
    private readonly IRepository<Domain.Entities.User> _userRepository;
    private readonly IMapper _mapper;

    public ReviewService(
        IRepository<Domain.Entities.Review> reviewRepository,
        IRepository<Domain.Entities.Place> placeRepository,
        IRepository<Domain.Entities.Event> eventRepository,
        IRepository<Domain.Entities.MediaAsset> mediaRepository,
        IRepository<Domain.Entities.User> userRepository,
        IMapper mapper)
    {
        _reviewRepository = reviewRepository;
        _placeRepository = placeRepository;
        _eventRepository = eventRepository;
        _mediaRepository = mediaRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<Result<ReviewResponse>> CreateAsync(CreateReviewRequest request, Guid userId)
    {
        if (!await IsResourceApproved(request.ResourceType, request.ResourceId))
            return Result.Fail<ReviewResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        var entity = new Domain.Entities.Review
        {
            ResourceType = request.ResourceType,
            ResourceId = request.ResourceId,
            UserId = userId,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            Status = ReviewStatus.Active
        };

        await _reviewRepository.AddAsync(entity);
        var response = _mapper.Map<ReviewResponse>(entity);
        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            response.UserFullName = user.FullName;
            response.UserAvatarUrl = user.AvatarUrl;
        }
        return Result.Ok(response, StatusCodes.Status201Created);
    }

    public async Task<Result<ReviewResponse>> UpdateAsync(Guid id, UpdateReviewRequest request, Guid userId)
    {
        var review = await _reviewRepository.GetByIdAsync(id);
        if (review == null)
            return Result.Fail<ReviewResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        if (review.UserId != userId)
            return Result.Fail<ReviewResponse>(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

        review.Rating = request.Rating;
        review.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        review.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        // Giữ review ở trạng thái Active sau khi user chỉnh sửa
        review.Status = ReviewStatus.Active;
        await _reviewRepository.UpdateAsync(review);
        var response = _mapper.Map<ReviewResponse>(review);
        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            response.UserFullName = user.FullName;
            response.UserAvatarUrl = user.AvatarUrl;
        }
        return Result.Ok(response);
    }

    public async Task<Result> DeleteAsync(Guid id, Guid userId, string role)
    {
        var review = await _reviewRepository.GetByIdAsync(id);
        if (review == null)
            return Result.Fail(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        if (review.UserId != userId && role != UserRole.Admin.ToString())
            return Result.Fail(AppConstants.ErrorMessages.Forbidden, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.Forbidden);

        await _reviewRepository.DeleteAsync(id);
        return Result.Ok("Review deleted successfully");
    }

    public async Task<Result<ReviewListResponse>> GetByResourceAsync(ResourceType resourceType, Guid resourceId, PaginationRequest request)
    {
        // Public chỉ thấy Active reviews
        var all = await _reviewRepository.FindAsync(
            r => r.ResourceType == resourceType && r.ResourceId == resourceId && r.Status == ReviewStatus.Active);
        var ordered = all.OrderByDescending(r => r.CreatedAt).ToList();
        var items = ordered.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();
        var responses = await MapReviewsWithUserInfo(items);

        var totalReviews = ordered.Count;
        var rated = ordered.Where(r => r.Rating.HasValue).Select(r => r.Rating!.Value).ToList();
        var averageRating = rated.Count > 0 ? Math.Round(rated.Average(), 1) : 0;

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
        // User thấy cả Active và Hidden reviews của mình
        var all = await _reviewRepository.FindAsync(
            r => r.ResourceType == resourceType && r.ResourceId == resourceId && r.UserId == userId
                 && (r.Status == ReviewStatus.Active || r.Status == ReviewStatus.Hidden));
        var ordered = all.OrderByDescending(r => r.CreatedAt).ToList();
        var items = ordered.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();
        var responses = await MapReviewsWithUserInfo(items);

        return Result.Ok(PaginationResponse<ReviewResponse>.Create(
            responses, ordered.Count, request.PageNumber, request.PageSize));
    }

    public async Task<Result<PaginationResponse<ReviewHistoryItemResponse>>> GetMyHistoryAsync(Guid userId, PaginationRequest request, ResourceType? resourceType)
    {
        var all = await _reviewRepository.FindAsync(r =>
            r.UserId == userId
            && (!resourceType.HasValue || r.ResourceType == resourceType.Value));

        var ordered = all.OrderByDescending(r => r.CreatedAt).ToList();
        var totalCount = ordered.Count;
        var items = ordered
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var user = await _userRepository.GetByIdAsync(userId);
        var placeIds = items.Where(r => r.ResourceType == ResourceType.Place).Select(r => r.ResourceId).Distinct().ToList();
        var eventIds = items.Where(r => r.ResourceType == ResourceType.Event).Select(r => r.ResourceId).Distinct().ToList();

        var places = placeIds.Count > 0
            ? (await _placeRepository.FindAsync(p => placeIds.Contains(p.Id))).ToDictionary(p => p.Id)
            : new Dictionary<Guid, Domain.Entities.Place>();

        var events = eventIds.Count > 0
            ? (await _eventRepository.FindAsync(e => eventIds.Contains(e.Id))).ToDictionary(e => e.Id)
            : new Dictionary<Guid, Domain.Entities.Event>();

        var resourceIds = placeIds.Concat(eventIds).Distinct().ToList();
        var media = resourceIds.Count > 0
            ? await _mediaRepository.FindAsync(m => resourceIds.Contains(m.ResourceId) && m.IsPrimary)
            : [];
        var mediaByResource = media
            .GroupBy(m => m.ResourceId)
            .ToDictionary(g => g.Key, g => g.FirstOrDefault());

        var responses = new List<ReviewHistoryItemResponse>();
        foreach (var review in items)
        {
            var response = new ReviewHistoryItemResponse
            {
                Id = review.Id,
                ResourceType = review.ResourceType,
                ResourceId = review.ResourceId,
                UserId = review.UserId,
                UserFullName = user?.FullName ?? string.Empty,
                UserAvatarUrl = user?.AvatarUrl ?? string.Empty,
                Rating = review.Rating,
                Comment = review.Comment,
                ImageUrl = review.ImageUrl,
                Status = review.Status,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            };

            if (review.ResourceType == ResourceType.Place && places.TryGetValue(review.ResourceId, out var place))
            {
                response.ResourceTitle = place.Title;
                response.ResourceAddress = place.Address;
            }
            else if (review.ResourceType == ResourceType.Event && events.TryGetValue(review.ResourceId, out var evt))
            {
                response.ResourceTitle = evt.Title;
                response.ResourceAddress = evt.Address;
            }

            if (mediaByResource.TryGetValue(review.ResourceId, out var resourceMedia) && resourceMedia != null)
                response.ResourceImageUrl = string.IsNullOrWhiteSpace(resourceMedia.SecureUrl) ? resourceMedia.Url : resourceMedia.SecureUrl;

            responses.Add(response);
        }

        return Result.Ok(PaginationResponse<ReviewHistoryItemResponse>.Create(
            responses, totalCount, request.PageNumber, request.PageSize));
    }

    // Admin: lấy tất cả reviews với filter theo status
    public async Task<Result<PaginationResponse<ReviewResponse>>> GetAllReviewsAsync(PaginationRequest request, ReviewStatus? statusFilter)
    {
        IEnumerable<Domain.Entities.Review> all;
        if (statusFilter.HasValue)
            all = await _reviewRepository.FindAsync(r => r.Status == statusFilter.Value);
        else
            all = await _reviewRepository.GetAllAsync();

        var ordered = all.OrderByDescending(r => r.CreatedAt).ToList();
        var totalCount = ordered.Count;
        var items = ordered.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList();
        var responses = await MapReviewsWithUserInfo(items);

        return Result.Ok(PaginationResponse<ReviewResponse>.Create(
            responses, totalCount, request.PageNumber, request.PageSize));
    }

    public async Task<Result> ApproveReviewAsync(Guid id)
    {
        var review = await _reviewRepository.GetByIdAsync(id);
        if (review == null)
            return Result.Fail(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        review.Status = ReviewStatus.Active;
        await _reviewRepository.UpdateAsync(review);
        return Result.Ok("Review approved successfully");
    }

    public async Task<Result> HideReviewAsync(Guid id)
    {
        var review = await _reviewRepository.GetByIdAsync(id);
        if (review == null)
            return Result.Fail(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        review.Status = ReviewStatus.Hidden;
        await _reviewRepository.UpdateAsync(review);
        return Result.Ok("Review hidden successfully");
    }

    private async Task<List<ReviewResponse>> MapReviewsWithUserInfo(List<Domain.Entities.Review> reviews)
    {
        var userIds = reviews.Select(r => r.UserId).Distinct().ToList();
        var users = await _userRepository.FindAsync(u => userIds.Contains(u.Id));
        var userDict = users.ToDictionary(u => u.Id);

        var responses = new List<ReviewResponse>();
        foreach (var review in reviews)
        {
            var response = _mapper.Map<ReviewResponse>(review);
            if (userDict.TryGetValue(review.UserId, out var user))
            {
                response.UserFullName = user.FullName;
                response.UserAvatarUrl = user.AvatarUrl;
            }
            responses.Add(response);
        }
        return responses;
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
