using System.Net;
using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests.Api.Admin;

public class CategoryTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    : BaseIntegrationTest(fixture, output)
{
    [Fact]
    public async Task Index_ReturnsOk_ForAdmin()
    {
        // Arrange
        await this.GivenAdminUserAsync();

        // Act
        var response = await HttpClient.GetAsync("/Admin/Category", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
    }

    [Fact]
    public async Task Create_ValidCategory_RedirectsToIndex()
    {
        // Arrange
        await this.GivenAdminUserAsync();

        var formData = new Dictionary<string, string>
        {
            ["Name"] = "New Category",
        };

        // Act
        var response = await HttpClient.PostAsync("/Admin/Category/Create", new FormUrlEncodedContent(formData), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBeOneOf(Redirect, SeeOther, MovedPermanently);

        // Verify DB
        var exists = await ExecuteDbContextAsync(async db =>
            await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Categories, c => c.Name == "New Category"));
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task Create_InvalidCategory_ReturnsViewWithErrors()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var formData = new Dictionary<string, string> { ["Name"] = "" };

        // Act
        var response = await HttpClient.PostAsync("/Admin/Category/Create", new FormUrlEncodedContent(formData), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("Category name is required");
    }
}