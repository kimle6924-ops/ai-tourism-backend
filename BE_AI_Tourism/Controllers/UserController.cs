using System.Security.Claims;
using BE_AI_Tourism.Application.DTOs.User;
using BE_AI_Tourism.Application.Services.User;
using BE_AI_Tourism.Shared.Constants;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BE_AI_Tourism.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IValidator<UpdateUserRequest> _updateUserValidator;
    private readonly IValidator<UpdatePreferencesRequest> _updatePreferencesValidator;

    public UserController(
        IUserService userService,
        IValidator<UpdateUserRequest> updateUserValidator,
        IValidator<UpdatePreferencesRequest> updatePreferencesValidator)
    {
        _userService = userService;
        _updateUserValidator = updateUserValidator;
        _updatePreferencesValidator = updatePreferencesValidator;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(AppConstants.JwtClaimTypes.UserId)?.Value;
        return Guid.Parse(userIdClaim!);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var result = await _userService.GetCurrentUserAsync(GetCurrentUserId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequest request)
    {
        var validation = await _updateUserValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(Shared.Core.Result.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var result = await _userService.UpdateProfileAsync(GetCurrentUserId(), request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("me/preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var result = await _userService.GetPreferencesAsync(GetCurrentUserId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("me/preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        var validation = await _updatePreferencesValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(Shared.Core.Result.Fail(string.Join("; ", validation.Errors.Select(e => e.ErrorMessage))));

        var result = await _userService.UpdatePreferencesAsync(GetCurrentUserId(), request);
        return StatusCode(result.StatusCode, result);
    }
}
