using BE_AI_Tourism.Application.DTOs.User;
using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using MapsterMapper;

namespace BE_AI_Tourism.Application.Services.User;

public class UserService : IUserService
{
    private readonly IRepository<Domain.Entities.User> _userRepository;
    private readonly IRepository<UserPreference> _preferenceRepository;
    private readonly IMapper _mapper;

    public UserService(
        IRepository<Domain.Entities.User> userRepository,
        IRepository<UserPreference> preferenceRepository,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _preferenceRepository = preferenceRepository;
        _mapper = mapper;
    }

    public async Task<Result<UserResponse>> GetCurrentUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return Result.Fail<UserResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        return Result.Ok(_mapper.Map<UserResponse>(user));
    }

    public async Task<Result<UserResponse>> UpdateProfileAsync(Guid userId, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return Result.Fail<UserResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        if (request.FullName != null) user.FullName = request.FullName;
        if (request.Phone != null) user.Phone = request.Phone;
        if (request.AvatarUrl != null) user.AvatarUrl = request.AvatarUrl;

        await _userRepository.UpdateAsync(user);
        return Result.Ok(_mapper.Map<UserResponse>(user));
    }

    public async Task<Result<PreferencesResponse>> GetPreferencesAsync(Guid userId)
    {
        var preference = await _preferenceRepository.FindOneAsync(p => p.UserId == userId);
        var response = new PreferencesResponse
        {
            CategoryIds = preference?.CategoryIds ?? []
        };
        return Result.Ok(response);
    }

    public async Task<Result<UserResponse>> UpdateLocationAsync(Guid userId, UpdateLocationRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return Result.Fail<UserResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        user.Latitude = request.Latitude;
        user.Longitude = request.Longitude;

        await _userRepository.UpdateAsync(user);
        return Result.Ok(_mapper.Map<UserResponse>(user));
    }

    public async Task<Result<PreferencesResponse>> UpdatePreferencesAsync(Guid userId, UpdatePreferencesRequest request)
    {
        var preference = await _preferenceRepository.FindOneAsync(p => p.UserId == userId);

        if (preference == null)
        {
            preference = new UserPreference
            {
                UserId = userId,
                CategoryIds = request.CategoryIds
            };
            await _preferenceRepository.AddAsync(preference);
        }
        else
        {
            preference.CategoryIds = request.CategoryIds;
            await _preferenceRepository.UpdateAsync(preference);
        }

        var response = new PreferencesResponse
        {
            CategoryIds = preference.CategoryIds
        };
        return Result.Ok(response);
    }
}
