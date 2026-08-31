using System.Threading.Tasks;
using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests.Api.Blog;

public class FeedTests(IntegrationTestFixture fixture)
    : BaseIntegrationTest(fixture)
{
    [Fact]
    public async Task Rss_ReturnsXmlContainingPublishedPosts()
    {
        // Arrange
        await this.GivenAuthenticatedUserAsync();
        var catId = await this.SeedCategoryAsync("Feed");
        var (_, slug) = await this.SeedPostAsync("Fed Into The Feed", catId, isPublished: true);
        await this.SeedPostAsync("Still A Draft", catId, isPublished: false);
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync("/feed.rss", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/rss+xml");

        var xml = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        xml.ShouldContain("<rss");
        xml.ShouldContain("Fed Into The Feed");
        xml.ShouldContain($"/blog/{slug}");
        xml.ShouldNotContain("Still A Draft");
    }

    [Fact]
    public async Task Index_LinksToTheFeedAndIncludesSocialMetaTags()
    {
        // Arrange
        await this.GivenAuthenticatedUserAsync();
        var catId = await this.SeedCategoryAsync("Meta");
        await this.SeedPostAsync("Meta Tagged Post", catId);
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync("/blog", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var document = await response.ParseHtmlAsync();

        document.QuerySelector("link[type='application/rss+xml']").ShouldNotBeNull();
        document.QuerySelector("meta[name='description']").ShouldNotBeNull();
        document.QuerySelector("meta[property='og:title']").ShouldNotBeNull();
        document.QuerySelector("link[rel='canonical']").ShouldNotBeNull();
    }

    [Fact]
    public async Task Post_WithFeaturedImage_IncludesOpenGraphImageTag()
    {
        // Arrange
        await this.GivenAuthenticatedUserAsync();
        var catId = await this.SeedCategoryAsync("OgImage");
        const string storedJson = """{"large":"/uploads/large.webp"}""";
        var (_, slug) = await this.SeedPostAsync("Post With Social Image", catId, isPublished: true, featuredImageUrls: storedJson);
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync($"/blog/{slug}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var document = await response.ParseHtmlAsync();

        var ogImage = document.QuerySelector("meta[property='og:image']");
        ogImage.ShouldNotBeNull();
        (ogImage!.GetAttribute("content") ?? string.Empty).ShouldContain("/uploads/large.webp");

        document.QuerySelector("meta[property='og:type']")!.GetAttribute("content").ShouldBe("article");
    }
}
