using System.Security.Claims;
using BE_AI_Tourism.Domain.Entities;

namespace BE_AI_Tourism.Application.Services.Auth;

public interface IJwtService
{
    string GenerateAccessToken(Domain.Entities.User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
