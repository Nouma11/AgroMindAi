using System.Net;
using System.Text.Json;

namespace AgricultureAPI.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var statusCode = ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? HttpStatusCode.NotFound
            : ex.Message.Contains("Invalid", StringComparison.OrdinalIgnoreCase)
                ? HttpStatusCode.Unauthorized
                : ex.Message.Contains("already in use", StringComparison.OrdinalIgnoreCase)
                    ? HttpStatusCode.Conflict
                    : HttpStatusCode.InternalServerError;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var result = JsonSerializer.Serialize(new
        {
            error = true,
            message = ex.Message,
            statusCode = (int)statusCode
        });

        return context.Response.WriteAsync(result);
    }
}