using BE_AI_Tourism.Application.DTOs.Auth;
using BE_AI_Tourism.Shared.Core;

namespace BE_AI_Tourism.Application.Services.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);
}
