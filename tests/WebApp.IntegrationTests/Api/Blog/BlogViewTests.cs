using System.Net;
using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using WebApp.Models;
using WebApp.Utils;

namespace WebApp.IntegrationTests.Api.Blog;

public class BlogViewTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    : BaseIntegrationTest(fixture, output)
{
    [Fact]
    public async Task Index_ReturnsOk_WithPosts()
    {
        // Arrange
        await this.GivenAuthenticatedUserAsync();
        var catId = await this.SeedCategoryAsync("Public");

        await this.SeedPostAsync("My First Post", catId);

        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync("/Blog", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("My First Post");
    }

    [Fact]
    public async Task Post_BySlug_ReturnsOk()
    {
        // Arrange
        await this.GivenAuthenticatedUserAsync();
        var catId = await this.SeedCategoryAsync("News");

        var (_, slug) = await this.SeedPostAsync("Breaking News", catId);

        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync($"/blog/{slug}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("Breaking News");
    }


    [Fact]
    public async Task Post_Unpublished_ReturnsNotFound_ForGuest()
    {
        // Arrange
        await this.GivenAuthenticatedUserAsync();
        var catId = await this.SeedCategoryAsync("Hidden");
        var slug = SlugHelper.GenerateSlug("Integration Test Post");

        await ExecuteDbContextAsync(async db =>
        {
            db.Posts.Add(new Post
            {
                Title = "Integration Test Post",
                Slug = slug,
                Content = "content",
                CreatedAt = DateTime.UtcNow,
                IsPublished = false
            });
            await db.SaveChangesAsync();
        });

        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync($"/blog/{slug}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}