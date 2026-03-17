using BE_AI_Tourism.Shared.Constants;

namespace BE_AI_Tourism.Shared.Core;

public class Result
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; }

    public static Result Ok(string? message = null) =>
        new() { Success = true, StatusCode = StatusCodes.Status200OK };

    public static Result Fail(string error, int statusCode = StatusCodes.Status400BadRequest) =>
        new() { Success = false, Error = error, StatusCode = statusCode };

    public static Result<T> Ok<T>(T data, int statusCode = StatusCodes.Status200OK) =>
        Result<T>.Ok(data, statusCode);

    public static Result<T> Fail<T>(string error, int statusCode = StatusCodes.Status400BadRequest) =>
        Result<T>.Fail(error, statusCode);

    public static Result NotFound(string error = AppConstants.ErrorMessages.NotFound) =>
        new() { Success = false, Error = error, StatusCode = StatusCodes.Status404NotFound };

    public static Result Unauthorized(string error = AppConstants.ErrorMessages.Unauthorized) =>
        new() { Success = false, Error = error, StatusCode = StatusCodes.Status401Unauthorized };
}

public class Result<T> : Result
{
    public T? Data { get; init; }

    public static Result<T> Ok(T data, int statusCode = StatusCodes.Status200OK) =>
        new() { Success = true, Data = data, StatusCode = statusCode };

    public new static Result<T> Fail(string error, int statusCode = StatusCodes.Status400BadRequest) =>
        new() { Success = false, Error = error, StatusCode = statusCode };
}
