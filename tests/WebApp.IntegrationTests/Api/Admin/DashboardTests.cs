using System.Threading.Tasks;
using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests.Api.Admin;

public class DashboardTests(IntegrationTestFixture fixture)
    : BaseIntegrationTest(fixture)
{
    [Fact]
    public async Task Index_AsAdmin_ReturnsDashboardWithCounts()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var categoryId = await this.SeedCategoryAsync("Tech");
        await this.SeedTagAsync("dotnet");
        await this.SeedPostAsync("Dashboard Test Post", categoryId);

        // Act
        var response = await HttpClient.GetAsync("/Admin/Dashboard", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("Total Posts");
    }
}
