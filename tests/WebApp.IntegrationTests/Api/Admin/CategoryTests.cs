using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests.Api.Admin;

public class CategoryTests(IntegrationTestFixture fixture)
    : BaseIntegrationTest(fixture)
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

        var createPage = await HttpClient.GetAsync("/Admin/Category/Create", TestContext.Current.CancellationToken);
        var token = await GetCsrfToken(createPage);

        var formData = new Dictionary<string, string>
        {
            ["Name"] = "New Category",
            ["__RequestVerificationToken"] = token
        };

        // Act
        var response = await HttpClient.PostAsync("/Admin/Category/Create", new FormUrlEncodedContent(formData), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBeOneOf(Redirect, SeeOther, MovedPermanently);

        // Assert persisted state
        var exists = await ExecuteDbContextAsync(async db =>
            await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Categories, c => c.Name == "New Category"));

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task Create_InvalidCategory_ReturnsViewWithErrors()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        
        var createPage = await HttpClient.GetAsync("/Admin/Category/Create", TestContext.Current.CancellationToken);
        var token = await GetCsrfToken(createPage);

        var formData = new Dictionary<string, string> 
        { 
            ["Name"] = "",
            ["__RequestVerificationToken"] = token
        };

        // Act
        var response = await HttpClient.PostAsync("/Admin/Category/Create", new FormUrlEncodedContent(formData), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("Category name is required");
    }
}