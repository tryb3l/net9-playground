using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests.Api.Admin;

public class AdminSecurityTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    : BaseIntegrationTest(fixture, output)
{
    [Theory]
    [InlineData("/Admin/Post")]
    [InlineData("/Admin/Post/Create")]
    public async Task ProtectedRoutes_ReturnUnauthorized_WhenAnonymous(string url)
    {
        // Arrange
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldNotBe(OK);
    }

    [Theory]
    [InlineData("/Admin/Post")]
    public async Task ProtectedRoutes_ReturnForbidden_WhenUserIsNotAdmin(string url)
    {
        // Arrange
        await this.GivenAuthenticatedUserAsync();

        // Act
        var response = await HttpClient.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(Forbidden);
    }
    
    [Fact]
    public async Task AdminRoutes_ReturnOk_WhenUserIsAdmin()
    {
        // Arrange
        await this.GivenAdminUserAsync();

        // Act
        var response = await HttpClient.GetAsync("/Admin/Post", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
    }
}