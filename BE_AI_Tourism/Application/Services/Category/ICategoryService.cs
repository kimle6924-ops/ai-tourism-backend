using BE_AI_Tourism.Application.DTOs.Category;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.Services.Category;

public interface ICategoryService
{
    Task<Result<CategoryResponse>> CreateAsync(CreateCategoryRequest request);
    Task<Result<CategoryResponse>> GetByIdAsync(Guid id);
    Task<Result<PaginationResponse<CategoryResponse>>> GetPagedAsync(PaginationRequest request);
    Task<Result<IEnumerable<CategoryResponse>>> GetActiveAsync();
    Task<Result<IEnumerable<CategoryResponse>>> GetByTypeAsync(string type);
    Task<Result<CategoryResponse>> UpdateAsync(Guid id, UpdateCategoryRequest request);
    Task<Result> DeleteAsync(Guid id);
}
