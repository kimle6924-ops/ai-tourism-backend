using BE_AI_Tourism.Application.DTOs.Auth;
using BE_AI_Tourism.Application.DTOs.User;
using BE_AI_Tourism.Configuration;
using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using MapsterMapper;
using Microsoft.Extensions.Options;

namespace BE_AI_Tourism.Application.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IRepository<Domain.Entities.User> _userRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordService _passwordService;
    private readonly IMapper _mapper;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        IRepository<Domain.Entities.User> userRepository,
        IJwtService jwtService,
        IPasswordService passwordService,
        IMapper mapper,
        IOptions<JwtOptions> jwtOptions)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _passwordService = passwordService;
        _mapper = mapper;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userRepository.FindOneAsync(u => u.Email == request.Email);
        if (existingUser != null)
            return Result.Fail<AuthResponse>(AppConstants.Auth.EmailAlreadyExists);

        var user = new Domain.Entities.User
        {
            Email = request.Email,
            Password = _passwordService.Hash(request.Password),
            FullName = request.FullName,
            Phone = request.Phone,
            Role = request.Role ?? UserRole.User,
            AdministrativeUnitId = request.AdministrativeUnitId,
            Status = UserStatus.Active
        };

        var refreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _userRepository.AddAsync(user);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationInMinutes),
            User = _mapper.Map<UserResponse>(user)
        };

        return Result.Ok(response, StatusCodes.Status201Created);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.FindOneAsync(u => u.Email == request.Email);
        if (user == null)
            return Result.Fail<AuthResponse>(AppConstants.Auth.InvalidCredentials, StatusCodes.Status401Unauthorized);

        if (!_passwordService.Verify(request.Password, user.Password))
            return Result.Fail<AuthResponse>(AppConstants.Auth.InvalidCredentials, StatusCodes.Status401Unauthorized);

        if (user.Status == UserStatus.Locked)
            return Result.Fail<AuthResponse>(AppConstants.Auth.AccountLocked, StatusCodes.Status403Forbidden);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationInMinutes),
            User = _mapper.Map<UserResponse>(user)
        };

        return Result.Ok(response);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var user = await _userRepository.FindOneAsync(u => u.RefreshToken == request.RefreshToken);
        if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return Result.Fail<AuthResponse>(AppConstants.Auth.InvalidRefreshToken, StatusCodes.Status401Unauthorized);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userRepository.UpdateAsync(user);

        var response = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationInMinutes),
            User = _mapper.Map<UserResponse>(user)
        };

        return Result.Ok(response);
    }
}
