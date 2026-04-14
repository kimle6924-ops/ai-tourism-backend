using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.DTOs.Discovery;

public class PlaceByTagsRequest : PaginationRequest
{
    public PlaceByTagsRequest()
    {
        PageSize = 16;
    }

    public List<string> Tags { get; set; } = [];
}
