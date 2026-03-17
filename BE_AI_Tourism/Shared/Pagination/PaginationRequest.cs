using BE_AI_Tourism.Shared.Constants;

namespace BE_AI_Tourism.Shared.Pagination;

public class PaginationRequest
{
    private int _pageNumber = AppConstants.Pagination.MinPageNumber;
    private int _pageSize = AppConstants.Pagination.DefaultPageSize;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < AppConstants.Pagination.MinPageNumber
            ? AppConstants.Pagination.MinPageNumber : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < AppConstants.Pagination.MinPageSize
            ? AppConstants.Pagination.MinPageSize
            : value > AppConstants.Pagination.MaxPageSize
                ? AppConstants.Pagination.MaxPageSize
                : value;
    }
}
