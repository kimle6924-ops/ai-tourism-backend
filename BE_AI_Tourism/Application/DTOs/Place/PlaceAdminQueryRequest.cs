using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.DTOs.Place;

public class PlaceAdminQueryRequest : PaginationRequest
{
    public Guid? ProvinceId { get; set; }
    public Guid? WardId { get; set; }
    public string? Q { get; set; }
}
