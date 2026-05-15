using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace WebApp.Utils;

public class AdminOrApiKeyRequirement : IAuthorizationRequirement { }

public class AdminOrApiKeyHandler : AuthorizationHandler<AdminOrApiKeyRequirement>
{
    public const string ApiKeyHeaderName = "X-Api-Key";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;

    public AdminOrApiKeyHandler(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminOrApiKeyRequirement requirement)
    {
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null && httpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyFromHeader))
        {
            var expectedApiKey = Environment.GetEnvironmentVariable("HEALTHCHECKS_API_KEY")
                ?? _configuration["HEALTHCHECKS_API_KEY"];
            var providedApiKey = apiKeyFromHeader.ToString();

            if (!string.IsNullOrEmpty(expectedApiKey) && IsValidApiKey(expectedApiKey, providedApiKey))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }

    private static bool IsValidApiKey(string expectedApiKey, string providedApiKey)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expectedApiKey);
        var providedBytes = Encoding.UTF8.GetBytes(providedApiKey);

        return expectedBytes.Length == providedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}