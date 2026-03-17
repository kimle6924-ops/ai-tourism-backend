using BE_AI_Tourism.Application.DTOs.User;
using BE_AI_Tourism.Domain.Enums;
using BE_AI_Tourism.Domain.Interfaces;
using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;
using BE_AI_Tourism.Shared.Pagination;
using MapsterMapper;

namespace BE_AI_Tourism.Application.Services.Admin;

public class AdminUserService : IAdminUserService
{
    private readonly IRepository<Domain.Entities.User> _userRepository;
    private readonly IMapper _mapper;

    public AdminUserService(IRepository<Domain.Entities.User> userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<Result<PaginationResponse<UserResponse>>> GetUsersAsync(PaginationRequest request)
    {
        var pagedUsers = await _userRepository.GetPagedAsync(request);
        var userResponses = pagedUsers.Items.Select(u => _mapper.Map<UserResponse>(u)).ToList();

        var response = PaginationResponse<UserResponse>.Create(
            userResponses, pagedUsers.TotalCount, pagedUsers.PageNumber, pagedUsers.PageSize);

        return Result.Ok(response);
    }

    public async Task<Result> LockUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return Result.Fail(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound);

        user.Status = UserStatus.Locked;
        await _userRepository.UpdateAsync(user);
        return Result.Ok("User locked successfully");
    }

    public async Task<Result> UnlockUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return Result.Fail(AppConstants.ErrorMessages.NotFound, StatusCodes.Status404NotFound);

        user.Status = UserStatus.Active;
        await _userRepository.UpdateAsync(user);
        return Result.Ok("User unlocked successfully");
    }
}
