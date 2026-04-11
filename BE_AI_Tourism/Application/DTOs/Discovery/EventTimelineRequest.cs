using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.DTOs.Discovery;

public class EventTimelineRequest : PaginationRequest
{
    public EventTimelineRequest()
    {
        PageSize = 16;
    }

    public string Timeline { get; set; } = "both";
    public double? RadiusKm { get; set; }
}
