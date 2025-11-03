using Shouldly;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests;

public class HomeControllerTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    : BaseIntegrationTest(fixture, output)
{
    [Fact]
    public async Task Index_ReturnsOk()
    {
        // Arrange/Act
        var response = await HttpClient.GetAsync("/", TestContext.Current.CancellationToken);
        
        // Assert
        response.StatusCode.ShouldBe(OK);
    }

    [Fact]
    public async Task NonExistentPage_ReturnsNotFound_AndLogsResponse()
    {
        // Arrange/Act
        var response = await HttpClient.GetAsync("/does-not-exist", TestContext.Current.CancellationToken);
        
        // Assert
        response.StatusCode.ShouldBe(expected: NotFound);
    }

    [Fact]
    public async Task CanAccessDbContext()
    {
        // Arrange/Act
        var canConnect = await ExecuteDbContextAsync(async db => await db.Database.CanConnectAsync());
        
        // Assert
        canConnect.ShouldBeTrue();
    }
}