using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace PROG2026_part2.Middleware;

/// <summary>
/// Catches any unhandled exception in the pipeline, logs it, and returns a clean
/// HTTP 500 "problem details" JSON response instead of leaking a stack trace to the caller.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            // Run the rest of the pipeline (controllers etc.).
            await _next(httpContext);
        }
        catch (Exception exception)
        {
            // Full details go to the server log only.
            _logger.LogError(
                exception,
                "An unexpected error occurred while processing {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path);

            await WriteErrorResponseAsync(
                httpContext,
                exception);
        }
    }

    private static async Task WriteErrorResponseAsync(
        HttpContext httpContext,
        Exception exception)
    {
        // If the response has already begun streaming, headers can no longer be changed.
        if (httpContext.Response.HasStarted)
        {
            throw exception;
        }

        httpContext.Response.Clear();
        httpContext.Response.StatusCode =
            (int)HttpStatusCode.InternalServerError;
        httpContext.Response.ContentType =
            "application/problem+json";

        // Generic message for the client; the trace ID lets the error be found in the logs.
        var problemDetails = new
        {
            type = "https://httpstatuses.com/500",
            title = "An unexpected error occurred.",
            status = StatusCodes.Status500InternalServerError,
            detail = "The server could not complete the request.",
            traceId = httpContext.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(problemDetails);

        await httpContext.Response.WriteAsync(json);
    }
}