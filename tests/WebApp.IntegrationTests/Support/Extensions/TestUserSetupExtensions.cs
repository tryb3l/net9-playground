using WebApp.IntegrationTests.Data;
using WebApp.IntegrationTests.Data.Builders;

namespace WebApp.IntegrationTests.Support.Extensions;

public static class TestUserSetupExtensions
{
    public static async Task<TestUser> GivenAuthenticatedUserAsync(
        this BaseIntegrationTest test, 
        Action<TestUserBuilder>? configure = null)
    {
        var builder = new TestUserBuilder().AsUser(); 
        configure?.Invoke(builder);
        var user = builder.Build();
        
        await test.SeedUserAsync(user);

        test.UserContext.CurrentUser = user;

        return user;
    }

    public static void GivenAnonymousUser(this BaseIntegrationTest test)
    {
        test.UserContext.CurrentUser = null;
    }

    public static Task<TestUser> GivenAdminUserAsync(this BaseIntegrationTest test)
        => test.GivenAuthenticatedUserAsync(b => b.AsAdmin());
}