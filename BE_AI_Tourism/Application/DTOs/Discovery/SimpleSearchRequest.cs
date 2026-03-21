using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.DTOs.Discovery;

public class SimpleSearchRequest : PaginationRequest
{
    public string? Search { get; set; }
    public string SortBy { get; set; } = "newest";
    public int? AverageRating { get; set; }
}
