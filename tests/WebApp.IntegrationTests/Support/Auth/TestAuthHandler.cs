using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WebApp.IntegrationTests.Support.Auth;

public class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    TestUserContext userContext)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string AuthenticationScheme = "TestScheme";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!userContext.IsAuthenticated || userContext.CurrentUser == null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, userContext.CurrentUser.Name),
            new(ClaimTypes.NameIdentifier, userContext.CurrentUser.Id),
            new(ClaimTypes.Email, userContext.CurrentUser.Email),
        };

        foreach (var role in userContext.CurrentUser.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}