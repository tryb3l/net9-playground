using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests.Api.Admin;

public class HealthcheckTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    : BaseIntegrationTest(fixture, output)
{
    [Fact]
    public async Task HealthUI_WhenAdmin_ReturnsOk()
    {
        // Arrange
        await this.GivenAdminUserAsync();

        // Act
        var response = await HttpClient.GetAsync("/health-ui", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
    }

    [Fact]
    public async Task HealthUI_WhenAnonymous_WithValidApiKey_ReturnsOk()
    {
        // Arrange
        this.GivenAnonymousUser();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health-ui");
        request.Headers.Add("X-Api-Key", "test-key");

        // Act
        var response = await HttpClient.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
    }

    [Fact]
    public async Task HealthUI_WhenAnonymous_NoKey_ReturnsForbiddenOrUnauthorized()
    {
        // Arrange
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync("/health-ui", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBeOneOf(Forbidden, Unauthorized);
    }
}