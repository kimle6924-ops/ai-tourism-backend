using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.DTOs.Review;

public class ReviewListResponse
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public PaginationResponse<ReviewResponse> Reviews { get; set; } = null!;
}
