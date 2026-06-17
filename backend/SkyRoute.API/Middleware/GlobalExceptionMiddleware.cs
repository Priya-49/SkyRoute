using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace SkyRoute.API.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private const string Rfc7807Type = "https://tools.ietf.org/html/rfc7807";

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while processing request.");
            await WriteServerErrorAsync(context);
        }
    }

    private static async Task WriteServerErrorAsync(HttpContext context)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Type = Rfc7807Type,
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "An unexpected error occurred. Please try again."
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
