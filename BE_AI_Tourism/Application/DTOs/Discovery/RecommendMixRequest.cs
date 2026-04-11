using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.DTOs.Discovery;

public class RecommendMixRequest : PaginationRequest
{
    public RecommendMixRequest()
    {
        PageSize = 9;
    }

    public double? MaxDistanceKm { get; set; }
}
