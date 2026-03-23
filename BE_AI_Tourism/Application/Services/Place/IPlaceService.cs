using BE_AI_Tourism.Application.DTOs.Place;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.Services.Place;

public interface IPlaceService
{
    Task<Result<PlaceResponse>> CreateAsync(CreatePlaceRequest request, Guid userId, string role, ContributorType? contributorType, Guid? userAdminUnitId);
    Task<Result<PlaceResponse>> GetByIdAsync(Guid id);
    Task<Result<PaginationResponse<PlaceResponse>>> GetApprovedPagedAsync(PaginationRequest request);
    Task<Result<PaginationResponse<PlaceResponse>>> GetAllPagedAsync(PaginationRequest request, string role, ContributorType? contributorType, Guid? userAdminUnitId);
    Task<Result<PlaceResponse>> UpdateAsync(Guid id, UpdatePlaceRequest request, Guid userId, string role, ContributorType? contributorType, Guid? userAdminUnitId);
    Task<Result> DeleteAsync(Guid id, Guid userId, string role, ContributorType? contributorType, Guid? userAdminUnitId);
    Task<Result<IEnumerable<PlaceResponse>>> SeedAsync();
}
