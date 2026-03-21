using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.DTOs.Discovery;

public class RecommendRequest : PaginationRequest
{
    public double? MaxDistanceKm { get; set; }
}
