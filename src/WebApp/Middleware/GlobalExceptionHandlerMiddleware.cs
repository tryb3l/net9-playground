using System.Net;
using System.Runtime.ExceptionServices;
using System.Text.Json;

namespace WebApp.Middleware;

public record ErrorDetails
{
    public required string Error { get; init; }
    public string? RequestId { get; init; }
    public string? StackTrace { get; init; }
}

public class GlobalExceptionHandlerMiddleware(
    ILogger<GlobalExceptionHandlerMiddleware> logger,
    IWebHostEnvironment environment) : IMiddleware
{
    private const string GenericErrorMessage = "An error occurred. Please try again later.";
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception has occurred while processing the request.");
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            logger.LogWarning("The response has already started, the global exception handler will not modify it.");
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        context.Response.Clear();
        context.Response.ContentType = "application/json";
        context.Response.Headers.CacheControl = "no-store, no-cache";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = environment.IsDevelopment()
            ? new ErrorDetails
            {
                Error = exception.Message,
                RequestId = context.TraceIdentifier,
                StackTrace = exception.StackTrace
            }
            : new ErrorDetails
            {
                Error = GenericErrorMessage,
                RequestId = context.TraceIdentifier
            };

        await context.Response.WriteAsJsonAsync(response, JsonSerializerOptions);
    }
}

public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}