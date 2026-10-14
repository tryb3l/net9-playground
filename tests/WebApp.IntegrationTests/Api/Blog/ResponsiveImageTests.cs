using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests.Api.Blog;

public class ResponsiveImageTests(IntegrationTestFixture fixture)
    : BaseIntegrationTest(fixture)
{
    private const string FullJson = """
        {"large":"/uploads/posts/t-large.webp","medium":"/uploads/posts/t-medium.webp","thumbnail":"/uploads/posts/t-thumbnail.webp"}
        """;

    private async Task<IDocument> GetPostPageAsync(string? featuredImageUrls)
    {
        await this.GivenAuthenticatedUserAsync();
        var catId = await this.SeedCategoryAsync("Images");
        var (_, slug) = await this.SeedPostAsync("Image Post", catId, isPublished: true,
            featuredImageUrls: featuredImageUrls);
        this.GivenAnonymousUser();

        var response = await HttpClient.GetAsync($"/blog/{slug}", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(OK);
        return await response.ParseHtmlAsync();
    }

    [Fact]
    public async Task Post_CoverImage_HasSrcsetWithAllVariantsAndEagerHighPriorityLoading()
    {
        var document = await GetPostPageAsync(FullJson);

        var cover = document.QuerySelector("img.post-cover-image");
        cover.ShouldNotBeNull();
        var srcset = cover.GetAttribute("srcset");
        srcset.ShouldNotBeNull();
        srcset.ShouldContain("/uploads/posts/t-thumbnail.webp 400w");
        srcset.ShouldContain("/uploads/posts/t-medium.webp 800w");
        srcset.ShouldContain("/uploads/posts/t-large.webp 1920w");
        cover.GetAttribute("sizes").ShouldNotBeNullOrWhiteSpace();
        cover.GetAttribute("fetchpriority").ShouldBe("high");
        cover.GetAttribute("decoding").ShouldBe("async");
        cover.GetAttribute("loading").ShouldNotBe("lazy");
    }

    [Fact]
    public async Task Post_BlurredBackground_UsesThumbnailInsteadOfLarge()
    {
        var document = await GetPostPageAsync(FullJson);

        var background = document.QuerySelector(".post-cover-background");
        background.ShouldNotBeNull();
        var style = background.GetAttribute("style");
        style.ShouldNotBeNull();
        style.ShouldContain("/uploads/posts/t-thumbnail.webp");
        style.ShouldNotContain("t-large.webp");
    }

    [Theory]
    [InlineData("/uploads/legacy.webp")]
    [InlineData("https://placehold.co/1920x1080")]
    public async Task Post_LegacyOrExternalSingleUrl_FallsBackToPlainSrcWithoutSrcset(string storedUrl)
    {
        var document = await GetPostPageAsync(storedUrl);

        var cover = document.QuerySelector("img.post-cover-image");
        cover.ShouldNotBeNull();
        cover.GetAttribute("src").ShouldBe(storedUrl);
        cover.HasAttribute("srcset").ShouldBeFalse();
    }

    [Fact]
    public async Task Index_PostCard_UsesMediumSrcWithSrcsetAndLazyAsyncLoading()
    {
        await this.GivenAuthenticatedUserAsync();
        var catId = await this.SeedCategoryAsync("Cards");
        await this.SeedPostAsync("Card Post", catId, isPublished: true, featuredImageUrls: FullJson);
        this.GivenAnonymousUser();

        var response = await HttpClient.GetAsync("/blog", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(OK);
        var document = await response.ParseHtmlAsync();

        var image = document.QuerySelectorAll("img.post-card-image").Single();
        image.GetAttribute("src").ShouldBe("/uploads/posts/t-medium.webp");
        var srcset = image.GetAttribute("srcset");
        srcset.ShouldNotBeNull();
        srcset.ShouldContain("/uploads/posts/t-thumbnail.webp 400w");
        srcset.ShouldContain("/uploads/posts/t-medium.webp 800w");
        srcset.ShouldContain("/uploads/posts/t-large.webp 1920w");
        image.GetAttribute("sizes").ShouldNotBeNullOrWhiteSpace();
        image.GetAttribute("loading").ShouldBe("lazy");
        image.GetAttribute("decoding").ShouldBe("async");
    }

    [Fact]
    public async Task Index_PostCardWithLegacySingleUrl_RendersImageWithoutSrcset()
    {
        await this.GivenAuthenticatedUserAsync();
        var catId = await this.SeedCategoryAsync("LegacyCards");
        await this.SeedPostAsync("Legacy Card Post", catId, isPublished: true,
            featuredImageUrls: "/uploads/legacy.webp");
        this.GivenAnonymousUser();

        var response = await HttpClient.GetAsync("/blog", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(OK);
        var document = await response.ParseHtmlAsync();

        var image = document.QuerySelectorAll("img.post-card-image").Single();
        image.GetAttribute("src").ShouldBe("/uploads/legacy.webp");
        image.HasAttribute("srcset").ShouldBeFalse();
        image.HasAttribute("sizes").ShouldBeFalse();
    }
}
