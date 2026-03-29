using TravelPlannerApp.Application.Common.Exceptions;

namespace TravelPlannerApp.Api.Common.Errors;

public sealed class AppExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AppExceptionMiddleware> _logger;

    public AppExceptionMiddleware(RequestDelegate next, ILogger<AppExceptionMiddleware> logger)
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
        catch (AppException exception)
        {
            _logger.LogWarning(
                "Application error {StatusCode} for {Method} {Path}: {Message}",
                exception.StatusCode,
                context.Request.Method,
                context.Request.Path,
                exception.Message);
            await WriteProblemAsync(context, exception.StatusCode, TitleFor(exception.StatusCode), exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
            await WriteProblemAsync(context, 500, "Internal Server Error", "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        var result = Results.Problem(statusCode: statusCode, title: title, detail: detail);
        await result.ExecuteAsync(context);
    }

    private static string TitleFor(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            412 => "Precondition Failed",
            428 => "Precondition Required",
            _ => "Error"
        };
    }
}
