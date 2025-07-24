using System.Net;
using System.Text.Json;

namespace WebApplication1.Middleware;

public class GlobalExceptionHandlerMiddleware(
    ILogger<GlobalExceptionHandlerMiddleware> logger,
    IWebHostEnvironment environment) : IMiddleware
{
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
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = environment.IsDevelopment()
            ? new { error = exception.Message, stackTrace = exception.StackTrace }
            : new { error = "An error occurred. Please try again later.", stackTrace = (string?)null };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(json);
    }
}

public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}