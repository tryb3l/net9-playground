using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests.Api.Home;

public class HomeIndexTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    : BaseIntegrationTest(fixture, output)
{
    [Fact]
    public async Task ReturnsOk_WhenAnonymous()
    {
        // Arrange
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync("/", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
    }

    [Fact]
    public async Task ReturnsOk_WhenUserIsAdmin()
    {
        // Arrange
        await this.GivenAdminUserAsync();

        // Act
        var response = await HttpClient.GetAsync("/", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
    }
}