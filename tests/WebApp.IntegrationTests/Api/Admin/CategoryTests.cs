using System.Net;
using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;

namespace WebApp.IntegrationTests.Api.Admin;

public class CategoryTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    : BaseIntegrationTest(fixture, output)
{
    [Fact]
    public async Task Index_ReturnsOk_ForAdmin()
    {
        await this.GivenAdminUserAsync();
        var response = await HttpClient.GetAsync("/Admin/Category", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
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
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.Redirect, HttpStatusCode.SeeOther, HttpStatusCode.MovedPermanently);
        
        // Verify DB
        var exists = await ExecuteDbContextAsync(async db => 
            await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Categories, c => c.Name == "New Category"));
        exists.ShouldBeTrue();
    }
}