using System.Net;
using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests.Api.Admin;

public class PostTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    : BaseIntegrationTest(fixture, output)
{
    [Fact]
    public async Task Index_WhenAdmin_ReturnsOk()
    {
        await this.GivenAdminUserAsync();
        var response = await HttpClient.GetAsync("/Admin/Post", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(OK);
    }

    [Fact]
    public async Task Create_Post_WhenAdmin_RedirectsToIndex()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var categoryId = await this.SeedCategoryAsync("Integration Test Category");

        var createModel = new Dictionary<string, string>
        {
            ["Title"] = "Integration Test Post",
            ["Content"] = "This is a test content",
            ["CategoryId"] = categoryId.ToString(),
            ["PublishNow"] = "true"
        };

        var content = new FormUrlEncodedContent(createModel);

        // Act
        var response = await HttpClient.PostAsync("/Admin/Post/Create", content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBeOneOf(Redirect, MovedPermanently, SeeOther);
    }

    [Fact]
    public async Task Details_WhenPostExists_ReturnsOk()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        
        var catId = await this.SeedCategoryAsync("Tech");
        
        var (postId, _) = await this.SeedPostAsync("Integration Testing 101", catId);

        // Act
        var response = await HttpClient.GetAsync($"/Admin/Post/Details/{postId}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
    }
}