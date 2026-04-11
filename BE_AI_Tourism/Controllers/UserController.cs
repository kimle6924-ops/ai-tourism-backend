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
    private readonly IValidator<UpdateAccountRequest> _updateAccountValidator;
    private readonly IValidator<UpdatePreferencesRequest> _updatePreferencesValidator;
    private readonly IValidator<UpdateLocationRequest> _updateLocationValidator;
    private readonly IValidator<FinalizeAvatarUploadRequest> _finalizeAvatarUploadValidator;

    public UserController(
        IUserService userService,
        IValidator<UpdateUserRequest> updateUserValidator,
        IValidator<UpdateAccountRequest> updateAccountValidator,
        IValidator<UpdatePreferencesRequest> updatePreferencesValidator,
        IValidator<UpdateLocationRequest> updateLocationValidator,
        IValidator<FinalizeAvatarUploadRequest> finalizeAvatarUploadValidator)
    {
        _userService = userService;
        _updateUserValidator = updateUserValidator;
        _updateAccountValidator = updateAccountValidator;
        _updatePreferencesValidator = updatePreferencesValidator;
        _updateLocationValidator = updateLocationValidator;
        _finalizeAvatarUploadValidator = finalizeAvatarUploadValidator;
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
            return BadRequest(Shared.Core.Result.ValidationFail(validation.Errors));

        var result = await _userService.UpdateProfileAsync(GetCurrentUserId(), request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("me/account")]
    public async Task<IActionResult> UpdateAccount([FromBody] UpdateAccountRequest request)
    {
        var validation = await _updateAccountValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(Shared.Core.Result.ValidationFail(validation.Errors));

        var result = await _userService.UpdateAccountAsync(GetCurrentUserId(), request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpGet("me/preferences")]
    public async Task<IActionResult> GetPreferences()
    {
        var result = await _userService.GetPreferencesAsync(GetCurrentUserId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("me/location")]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
    {
        var validation = await _updateLocationValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(Shared.Core.Result.ValidationFail(validation.Errors));

        var result = await _userService.UpdateLocationAsync(GetCurrentUserId(), request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPut("me/preferences")]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request)
    {
        var validation = await _updatePreferencesValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(Shared.Core.Result.ValidationFail(validation.Errors));

        var result = await _userService.UpdatePreferencesAsync(GetCurrentUserId(), request);
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("me/avatar/upload-signature")]
    public async Task<IActionResult> GenerateAvatarUploadSignature()
    {
        var result = await _userService.GenerateAvatarUploadSignatureAsync(GetCurrentUserId());
        return StatusCode(result.StatusCode, result);
    }

    [HttpPost("me/avatar/finalize")]
    public async Task<IActionResult> FinalizeAvatarUpload([FromBody] FinalizeAvatarUploadRequest request)
    {
        var validation = await _finalizeAvatarUploadValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(Shared.Core.Result.ValidationFail(validation.Errors));

        var result = await _userService.FinalizeAvatarUploadAsync(GetCurrentUserId(), request);
        return StatusCode(result.StatusCode, result);
    }
}
