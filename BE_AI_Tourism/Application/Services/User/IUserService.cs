using BE_AI_Tourism.Application.DTOs.User;
using BE_AI_Tourism.Shared.Core;

namespace BE_AI_Tourism.Application.Services.User;

public interface IUserService
{
    Task<Result<UserResponse>> GetCurrentUserAsync(Guid userId);
    Task<Result<UserResponse>> UpdateProfileAsync(Guid userId, UpdateUserRequest request);
    Task<Result<PreferencesResponse>> GetPreferencesAsync(Guid userId);
    Task<Result<PreferencesResponse>> UpdatePreferencesAsync(Guid userId, UpdatePreferencesRequest request);
    Task<Result<UserResponse>> UpdateLocationAsync(Guid userId, UpdateLocationRequest request);
}
