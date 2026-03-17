using BE_AI_Tourism.Shared.Constants;
using BE_AI_Tourism.Shared.Core;

namespace BE_AI_Tourism.Middlewares;

// Global exception handler - controllers don't need try/catch
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            KeyNotFoundException => (StatusCodes.Status404NotFound, AppConstants.ErrorMessages.NotFound),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, AppConstants.ErrorMessages.Unauthorized),
            ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, AppConstants.ErrorMessages.InternalError)
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var result = Result.Fail(message, statusCode);
        await context.Response.WriteAsJsonAsync(result);
    }
}
