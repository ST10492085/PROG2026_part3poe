using System.Diagnostics;

namespace PROG2026_part2.Middleware;

/// <summary>
/// Logs the HTTP method, path, status code and duration of every request.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        // Start timing before the request is processed.
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(httpContext);
        }
        finally
        {
            // 'finally' ensures the request is logged even if it threw an exception.
            stopwatch.Stop();

            _logger.LogInformation(
                "HTTP {Method} {Path} returned {StatusCode} in {ElapsedMilliseconds} ms.",
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}