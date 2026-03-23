using System.Diagnostics;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Api.Middleware;

public class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger, IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning("Request was canceled by the client. TraceId: {TraceId}", Activity.Current?.Id ?? context.TraceIdentifier);
        }
        catch (Exception exception)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", traceId);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var message = environment.IsDevelopment()
                ? exception.Message
                : "An unexpected error occurred while processing the request.";

            var payload = ApiResponse<ApiErrorDetails>.Failure(
                message,
                new ApiErrorDetails
                {
                    TraceId = traceId,
                    Errors = environment.IsDevelopment()
                        ? [exception.GetType().Name]
                        : Array.Empty<string>()
                });

            await context.Response.WriteAsJsonAsync(payload);
        }
    }
}
