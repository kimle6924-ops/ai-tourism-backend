using BE_AI_Tourism.Application.DTOs.User;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;

namespace BE_AI_Tourism.Application.Services.Admin;

public interface IAdminUserService
{
    Task<Result<PaginationResponse<UserResponse>>> GetUsersAsync(PaginationRequest request);
    Task<Result> LockUserAsync(Guid userId);
    Task<Result> UnlockUserAsync(Guid userId);
    Task<Result> ApproveUserAsync(Guid userId);
}
