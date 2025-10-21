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
            var expectedApiKey = _configuration["HEALTHCHECKS_API_KEY"];
            if (!string.IsNullOrEmpty(expectedApiKey) && expectedApiKey.Equals(apiKeyFromHeader))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }
}