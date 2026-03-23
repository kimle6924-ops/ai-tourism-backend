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
    private readonly IRepository<AdministrativeUnit> _adminUnitRepository;
    private readonly IRepository<UserPreference> _preferenceRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordService _passwordService;
    private readonly IMapper _mapper;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        IRepository<Domain.Entities.User> userRepository,
        IRepository<AdministrativeUnit> adminUnitRepository,
        IRepository<UserPreference> preferenceRepository,
        IJwtService jwtService,
        IPasswordService passwordService,
        IMapper mapper,
        IOptions<JwtOptions> jwtOptions)
    {
        _userRepository = userRepository;
        _adminUnitRepository = adminUnitRepository;
        _preferenceRepository = preferenceRepository;
        _jwtService = jwtService;
        _passwordService = passwordService;
        _mapper = mapper;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        // Chặn client tự tạo Admin (defense in depth — validator cũng chặn)
        var role = request.Role ?? UserRole.User;
        if (role == UserRole.Admin)
            return Result.Fail<AuthResponse>(AppConstants.Auth.CannotRegisterAdmin, StatusCodes.Status400BadRequest, AppConstants.ErrorCodes.CannotRegisterAdmin);

        // Chuẩn hóa email trước khi check trùng
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _userRepository.FindOneAsync(u => u.Email == normalizedEmail);
        if (existingUser != null)
            return Result.Fail<AuthResponse>(AppConstants.Auth.EmailAlreadyExists, StatusCodes.Status409Conflict, AppConstants.ErrorCodes.EmailAlreadyExists);

        // Nếu không phải Contributor thì bỏ qua AdministrativeUnitId và ContributorType
        if (role != UserRole.Contributor)
        {
            request.AdministrativeUnitId = null;
            request.ContributorType = null;
        }

        // Kiểm tra logic Contributor theo ContributorType
        if (role == UserRole.Contributor)
        {
            if (!request.ContributorType.HasValue)
                return Result.Fail<AuthResponse>("ContributorType is required for Contributor", StatusCodes.Status400BadRequest, "CONTRIBUTOR_TYPE_REQUIRED");

            var contributorType = request.ContributorType.Value;

            // Central không cần AdministrativeUnitId
            if (contributorType == ContributorType.Central)
            {
                request.AdministrativeUnitId = null;
            }
            else
            {
                // Province, Ward, Collaborator cần AdministrativeUnitId
                if (!request.AdministrativeUnitId.HasValue)
                    return Result.Fail<AuthResponse>(AppConstants.Auth.ContributorRequiresAdminUnit, StatusCodes.Status400BadRequest, AppConstants.ErrorCodes.ContributorRequiresAdminUnit);

                var adminUnit = await _adminUnitRepository.GetByIdAsync(request.AdministrativeUnitId.Value);
                if (adminUnit == null)
                    return Result.Fail<AuthResponse>(AppConstants.Auth.AdminUnitNotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.AdminUnitNotFound);

                // Validate level phù hợp với ContributorType
                if (contributorType == ContributorType.Province && adminUnit.Level != AdministrativeLevel.Province)
                    return Result.Fail<AuthResponse>("Province contributor must select a Province-level unit", StatusCodes.Status400BadRequest, "INVALID_ADMIN_UNIT_LEVEL");

                if ((contributorType == ContributorType.Ward || contributorType == ContributorType.Collaborator) && adminUnit.Level != AdministrativeLevel.Ward)
                    return Result.Fail<AuthResponse>("Ward/Collaborator contributor must select a Ward-level unit", StatusCodes.Status400BadRequest, "INVALID_ADMIN_UNIT_LEVEL");
            }
        }

        var user = new Domain.Entities.User
        {
            Email = normalizedEmail,
            Password = _passwordService.Hash(request.Password),
            FullName = request.FullName.Trim(),
            Phone = request.Phone?.Trim() ?? string.Empty,
            Role = role,
            ContributorType = request.ContributorType,
            AdministrativeUnitId = request.AdministrativeUnitId,
            Status = role == UserRole.Contributor ? UserStatus.PendingApproval : UserStatus.Active
        };

        var refreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await _userRepository.AddAsync(user);

        // Tạo UserPreference cùng lúc khi đăng ký
        if (request.CategoryIds.Any())
        {
            var preference = new UserPreference
            {
                UserId = user.Id,
                CategoryIds = request.CategoryIds
            };
            await _preferenceRepository.AddAsync(preference);
        }

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
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _userRepository.FindOneAsync(u => u.Email == normalizedEmail);
        if (user == null)
            return Result.Fail<AuthResponse>(AppConstants.Auth.InvalidCredentials, StatusCodes.Status401Unauthorized, AppConstants.ErrorCodes.InvalidCredentials);

        if (!_passwordService.Verify(request.Password, user.Password))
            return Result.Fail<AuthResponse>(AppConstants.Auth.InvalidCredentials, StatusCodes.Status401Unauthorized, AppConstants.ErrorCodes.InvalidCredentials);

        if (user.Status == UserStatus.Locked)
            return Result.Fail<AuthResponse>(AppConstants.Auth.AccountLocked, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.AccountLocked);

        if (user.Status == UserStatus.PendingApproval)
            return Result.Fail<AuthResponse>(AppConstants.Auth.AccountPendingApproval, StatusCodes.Status403Forbidden, AppConstants.ErrorCodes.AccountPendingApproval);

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
            return Result.Fail<AuthResponse>(AppConstants.Auth.InvalidRefreshToken, StatusCodes.Status401Unauthorized, AppConstants.ErrorCodes.InvalidRefreshToken);

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
