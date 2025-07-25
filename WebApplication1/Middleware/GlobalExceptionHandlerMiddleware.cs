using System.Net;
using System.Text.Json;

namespace WebApplication1.Middleware;

public record ErrorDetails
{
    public required string Error { get; init; }
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
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = environment.IsDevelopment()
            ? new ErrorDetails { Error = exception.Message, StackTrace = exception.StackTrace }
            : new ErrorDetails { Error = GenericErrorMessage };

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