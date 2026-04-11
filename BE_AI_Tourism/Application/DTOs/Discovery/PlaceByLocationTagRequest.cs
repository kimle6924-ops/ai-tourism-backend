using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.DTOs.Discovery;

public class PlaceByLocationTagRequest : PaginationRequest
{
    public PlaceByLocationTagRequest()
    {
        PageSize = 16;
    }

    public string Tag { get; set; } = string.Empty;
    public double? RadiusKm { get; set; }
}
