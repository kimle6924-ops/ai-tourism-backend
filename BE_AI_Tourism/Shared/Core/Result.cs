using BE_AI_Tourism.Shared.Constants;
using FluentValidation.Results;

namespace BE_AI_Tourism.Shared.Core;

public class Result
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public string? ErrorCode { get; init; }
    public int StatusCode { get; init; }
    public Dictionary<string, List<string>>? Errors { get; init; }

    public static Result Ok(string? message = null) =>
        new() { Success = true, StatusCode = StatusCodes.Status200OK };

    public static Result Fail(string error, int statusCode = StatusCodes.Status400BadRequest, string? errorCode = null) =>
        new() { Success = false, Error = error, ErrorCode = errorCode, StatusCode = statusCode };

    public static Result ValidationFail(Dictionary<string, List<string>> errors) =>
        new() { Success = false, Error = "Validation failed", ErrorCode = "VALIDATION_FAILED", Errors = errors, StatusCode = StatusCodes.Status400BadRequest };

    public static Result ValidationFail(IList<ValidationFailure> failures)
    {
        var errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToList());
        return ValidationFail(errors);
    }

    public static Result<T> Ok<T>(T data, int statusCode = StatusCodes.Status200OK) =>
        Result<T>.Ok(data, statusCode);

    public static Result<T> Fail<T>(string error, int statusCode = StatusCodes.Status400BadRequest, string? errorCode = null) =>
        Result<T>.Fail(error, statusCode, errorCode);

    public static Result NotFound(string error = AppConstants.ErrorMessages.NotFound) =>
        new() { Success = false, Error = error, ErrorCode = "NOT_FOUND", StatusCode = StatusCodes.Status404NotFound };

    public static Result Unauthorized(string error = AppConstants.ErrorMessages.Unauthorized) =>
        new() { Success = false, Error = error, ErrorCode = "UNAUTHORIZED", StatusCode = StatusCodes.Status401Unauthorized };
}

public class Result<T> : Result
{
    public T? Data { get; init; }

    public static Result<T> Ok(T data, int statusCode = StatusCodes.Status200OK) =>
        new() { Success = true, Data = data, StatusCode = statusCode };

    public new static Result<T> Fail(string error, int statusCode = StatusCodes.Status400BadRequest, string? errorCode = null) =>
        new() { Success = false, Error = error, ErrorCode = errorCode, StatusCode = statusCode };
}
