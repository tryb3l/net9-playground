using System;
using System.Threading.Tasks;
using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests.Api.Blog;

public class BlogViewTests(IntegrationTestFixture fixture)
    : BaseIntegrationTest(fixture)
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
        
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("My First Post");
    }

    [Fact]
    public async Task Index_WithMultiplePages_ShowsCorrectPagination()
    {
        // Arrange
        await this.GivenAuthenticatedUserAsync();
        var catId = await this.SeedCategoryAsync("Pagination Test");
        
        // Seed posts with different dates
        for (var i = 1; i <= 7; i++)
        {
            await this.SeedPostAsync(
                $"Pagination Post {i}", 
                catId, 
                createdAt: DateTime.UtcNow.AddDays(-i));
        }
        this.GivenAnonymousUser();

        // Act
        var responsePage1 = await HttpClient.GetAsync("/blog?page=1", TestContext.Current.CancellationToken);
        
        var responsePage2 = await HttpClient.GetAsync("/blog?page=2", TestContext.Current.CancellationToken);

        // Assert
        responsePage1.StatusCode.ShouldBe(OK);
        responsePage2.StatusCode.ShouldBe(OK);

        var contentPage1 = await responsePage1.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var contentPage2 = await responsePage2.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Verify different posts on different pages
        contentPage1.ShouldContain("Pagination Post");
        contentPage2.ShouldContain("Pagination Post");
        
        contentPage1.ShouldNotBe(contentPage2);
    }

    [Theory]
    [InlineData("C# 10 Features!", "c-10-features")]
    [InlineData("What's New in .NET?", "whats-new-in-net")]
    [InlineData("100% Pure JavaScript", "100-pure-javascript")]
    public async Task Post_WithSpecialCharactersInTitle_ResolvesCorrectly(string title, string expectedSlugPattern)
    {
        // Arrange
        await this.GivenAuthenticatedUserAsync();
        var catId = await this.SeedCategoryAsync("Special Chars");
        var (_, slug) = await this.SeedPostAsync(title, catId);
        this.GivenAnonymousUser();

        // Assert slug follows expected pattern
        slug.ShouldStartWith(expectedSlugPattern);

        // Act
        var response = await HttpClient.GetAsync($"/blog/{slug}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        
        var document = await response.ParseHtmlAsync();
        var titleElement = document.QuerySelector("h1, .post-full-title");
        titleElement.ShouldNotBeNull();
        titleElement!.TextContent.ShouldContain(title);
    }

    [Fact]
    public async Task Index_WithCategoryAndTagFilters_ShowsOnlyMatchingPosts()
    {
        // Arrange
        await this.GivenAuthenticatedUserAsync();
        var techId = await this.SeedCategoryAsync("Tech");
        var lifeId = await this.SeedCategoryAsync("Life");
        var dotnetTagId = await this.SeedTagAsync("dotnet");
        await this.SeedPostAsync("Tagged DotNet Post", techId, tagIds: [dotnetTagId]);
        await this.SeedPostAsync("Cooking Post", lifeId);
        this.GivenAnonymousUser();

        // Act
        var matchingResponse = await HttpClient.GetAsync("/blog?category=Tech&tag=dotnet", TestContext.Current.CancellationToken);
        var wrongTagResponse = await HttpClient.GetAsync("/blog?tag=cooking", TestContext.Current.CancellationToken);
        var otherCategoryResponse = await HttpClient.GetAsync("/blog?category=Life", TestContext.Current.CancellationToken);

        // Assert
        matchingResponse.StatusCode.ShouldBe(OK);
        wrongTagResponse.StatusCode.ShouldBe(OK);
        otherCategoryResponse.StatusCode.ShouldBe(OK);

        var matchingContent = await matchingResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        matchingContent.ShouldContain("Tagged DotNet Post");
        matchingContent.ShouldNotContain("Cooking Post");

        var wrongTagContent = await wrongTagResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        wrongTagContent.ShouldNotContain("Tagged DotNet Post");

        var otherCategoryContent = await otherCategoryResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        otherCategoryContent.ShouldContain("Cooking Post");
        otherCategoryContent.ShouldNotContain("Tagged DotNet Post");
    }
}