using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SkyRoute.API.Exceptions;

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
            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        var (statusCode, title, detail, errors) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                validationException.Message,
                validationException.Errors),
            NotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                "Not Found",
                notFoundException.Message,
                (IDictionary<string, string[]>?)null),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please try again.",
                (IDictionary<string, string[]>?)null)
        };

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Type = Rfc7807Type,
            Title = title,
            Status = statusCode,
            Detail = detail
        };

        var payload = errors is null
            ? JsonSerializer.Serialize(problem)
            : JsonSerializer.Serialize(new ValidationProblemDetails(errors)
            {
                Type = Rfc7807Type,
                Title = title,
                Status = statusCode,
                Detail = detail
            });

        await context.Response.WriteAsync(payload);
    }
}
