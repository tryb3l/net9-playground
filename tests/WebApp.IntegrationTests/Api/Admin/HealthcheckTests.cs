using System.Net.Http;
using System.Threading.Tasks;
using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests.Api.Admin;

public class HealthcheckTests(IntegrationTestFixture fixture)
    : BaseIntegrationTest(fixture)
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
    public async Task HealthEndpoint_WhenAnonymous_WithValidApiKey_ReturnsOk()
    {
        // Arrange
        this.GivenAnonymousUser();

        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Api-Key", "test-key");

        // Act
        var response = await HttpClient.SendAsync(request, TestContext.Current.CancellationToken);

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
    public async Task HealthEndpoint_WhenAnonymous_NoKey_ReturnsForbiddenOrUnauthorized()
    {
        // Arrange
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync("/health", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBeOneOf(Forbidden, Unauthorized);
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

    [Fact]
    public async Task HealthLive_WhenAnonymous_ReturnsOk()
    {
        // Arrange
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync("/health/live", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
    }
}