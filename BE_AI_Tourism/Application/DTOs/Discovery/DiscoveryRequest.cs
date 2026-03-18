using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.DTOs.Discovery;

public class DiscoveryRequest : PaginationRequest
{
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? AdministrativeUnitId { get; set; }
    public string? Tag { get; set; }
    public string SortBy { get; set; } = "newest";
}
