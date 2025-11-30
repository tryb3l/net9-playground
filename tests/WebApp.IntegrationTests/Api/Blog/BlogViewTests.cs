using System.Net;
using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using WebApp.Models;
using WebApp.Utils;
using static System.Net.HttpStatusCode;

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
        var response = await HttpClient.GetAsync("/blog", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        
        var document = await response.ParseHtmlAsync();
        
        var postCards = document.QuerySelectorAll(".post-card, article, .posts-grid > *");
        postCards.Length.ShouldBeGreaterThan(0);
        
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("My First Post");
    }

    [Fact]
    public async Task Post_BySlug_ReturnsOk()
    {
        // Arrange
        await this.GivenAuthenticatedUserAsync();
        var catId = await this.SeedCategoryAsync("News");

        var (_, slug) = await this.SeedPostAsync("News Post", catId);

        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync($"/blog/{slug}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("News Post");
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
        response.StatusCode.ShouldBe(NotFound);
    }

    [Fact]
    public async Task Index_WithCategoryFilter_ShowsOnlyMatchingPosts()
    {
        // Arrange
        await this.GivenAuthenticatedUserAsync();
        const string tech = "Tech";
        const string testing = "Testing";
        var techId = await this.SeedCategoryAsync(tech);
        var testingId = await this.SeedCategoryAsync(testing);
        await this.SeedPostAsync("C# Tips", techId);
        await this.SeedPostAsync("Test Integration Post", testingId);
        this.GivenAnonymousUser();

        // Act
        var responseTech = await HttpClient.GetAsync($"/blog?category={tech}", TestContext.Current.CancellationToken);
        var responseTesting = await HttpClient.GetAsync($"/blog?category={testing}", TestContext.Current.CancellationToken);

        // Assert
        var contentTech = await responseTech.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var contentTesting = await responseTesting.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        responseTech.StatusCode.ShouldBe(OK);
        responseTesting.StatusCode.ShouldBe(OK);
        contentTech.ShouldContain("C# Tips");
        contentTesting.ShouldContain("Test Integration Post");
    }

    [Fact]
    public async Task Post_SoftDeleted_ReturnsNotFound()
    {
        // Arrange
        await this.GivenAuthenticatedUserAsync();
        var catId = await this.SeedCategoryAsync("General");
        var (id, slug) = await this.SeedPostAsync("Deleted Post", catId);

        await ExecuteDbContextAsync(async db =>
        {
            var p = await db.Posts.FindAsync(id);
            p?.IsDeleted = true;
            await db.SaveChangesAsync();
        });

        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync($"/blog/{slug}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(NotFound);
    }
}