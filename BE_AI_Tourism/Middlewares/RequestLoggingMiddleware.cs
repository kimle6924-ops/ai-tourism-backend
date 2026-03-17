using System.Diagnostics;

namespace BE_AI_Tourism.Middlewares;

// Log incoming requests and response time
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;

        _logger.LogInformation("Request: {Method} {Path}", method, path);

        await _next(context);

        stopwatch.Stop();
        _logger.LogInformation("Response: {Method} {Path} - {StatusCode} in {ElapsedMs}ms",
            method, path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
    }
}
