using BE_AI_Tourism.Application.DTOs.User;
using BE_AI_Tourism.Configuration;
using BE_AI_Tourism.Domain.Entities;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Infrastructure.Cloudinary;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using MapsterMapper;
using Microsoft.Extensions.Options;

namespace BE_AI_Tourism.Application.Services.User;

public class UserService : IUserService
{
    private readonly IRepository<Domain.Entities.User> _userRepository;
    private readonly IRepository<UserPreference> _preferenceRepository;
    private readonly ICloudinaryProvider _cloudinaryProvider;
    private readonly IMapper _mapper;
    private readonly CloudinaryOptions _cloudinaryOptions;

    public UserService(
        IRepository<Domain.Entities.User> userRepository,
        IRepository<UserPreference> preferenceRepository,
        ICloudinaryProvider cloudinaryProvider,
        IMapper mapper,
        IOptions<CloudinaryOptions> cloudinaryOptions)
    {
        _userRepository = userRepository;
        _preferenceRepository = preferenceRepository;
        _cloudinaryProvider = cloudinaryProvider;
        _mapper = mapper;
        _cloudinaryOptions = cloudinaryOptions.Value;
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

    public async Task<Result<UserResponse>> UpdateAccountAsync(Guid userId, UpdateAccountRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return Result.Fail<UserResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        if (request.Email != null)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var existing = await _userRepository.FindOneAsync(u => u.Email == normalizedEmail && u.Id != userId);
            if (existing != null)
                return Result.Fail<UserResponse>(AppConstants.Auth.EmailAlreadyExists, StatusCodes.Status409Conflict, AppConstants.ErrorCodes.EmailAlreadyExists);

            user.Email = normalizedEmail;
        }

        if (request.FullName != null) user.FullName = request.FullName;
        if (request.Phone != null) user.Phone = request.Phone;

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

    public async Task<Result<AvatarUploadSignatureResponse>> GenerateAvatarUploadSignatureAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return Result.Fail<AvatarUploadSignatureResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        var folder = $"{_cloudinaryOptions.Folder}/User/{userId}/Avatar";
        var (signature, timestamp) = _cloudinaryProvider.GenerateSignature(folder);

        return Result.Ok(new AvatarUploadSignatureResponse
        {
            Signature = signature,
            Timestamp = timestamp,
            ApiKey = _cloudinaryOptions.ApiKey,
            CloudName = _cloudinaryOptions.CloudName,
            Folder = folder
        });
    }

    public async Task<Result<UserResponse>> FinalizeAvatarUploadAsync(Guid userId, FinalizeAvatarUploadRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return Result.Fail<UserResponse>(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound, AppConstants.ErrorCodes.NotFound);

        user.AvatarUrl = string.IsNullOrWhiteSpace(request.SecureUrl) ? request.Url : request.SecureUrl;
        user.AvatarPublicId = request.PublicId;

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
