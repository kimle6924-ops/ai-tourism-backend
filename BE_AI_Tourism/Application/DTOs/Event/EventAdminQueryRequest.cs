using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.DTOs.Event;

public class EventAdminQueryRequest : PaginationRequest
{
    public Guid? ProvinceId { get; set; }
    public Guid? WardId { get; set; }
    public string? Q { get; set; }
}
