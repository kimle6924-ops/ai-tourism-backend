using BE_AI_Tourism.Application.DTOs.Administrative;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.Services.Administrative;

public interface IAdministrativeUnitService
{
    Task<Result<AdministrativeUnitResponse>> CreateAsync(CreateAdministrativeUnitRequest request);
    Task<Result<AdministrativeUnitResponse>> GetByIdAsync(Guid id);
    Task<Result<PaginationResponse<AdministrativeUnitResponse>>> GetPagedAsync(PaginationRequest request);
    Task<Result<IEnumerable<AdministrativeUnitResponse>>> GetByLevelAsync(AdministrativeLevel level);
    Task<Result<IEnumerable<AdministrativeUnitResponse>>> GetChildrenAsync(Guid parentId);
    Task<Result<AdministrativeUnitResponse>> UpdateAsync(Guid id, UpdateAdministrativeUnitRequest request);
    Task<Result> DeleteAsync(Guid id);
}
