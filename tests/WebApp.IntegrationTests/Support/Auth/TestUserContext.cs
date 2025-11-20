using WebApp.IntegrationTests.Data;

namespace WebApp.IntegrationTests.Support.Auth;

public class TestUserContext
{
    public TestUser? CurrentUser { get; set; }
    public bool IsAuthenticated => CurrentUser is not null;
}