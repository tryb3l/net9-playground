using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

    [Theory]
    [InlineData("0")]
    [InlineData("-3")]
    public async Task Index_WithNonPositivePage_DoesNotError(string page)
    {
        // (page - 1) * pageSize produced a negative OFFSET, which Postgres rejects.

        // Arrange
        await this.GivenAuthenticatedUserAsync();
        var catId = await this.SeedCategoryAsync("Clamp");
        await this.SeedPostAsync("Clamped Post", catId);
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync($"/blog?page={page}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("Clamped Post");
    }

    [Fact]
    public async Task Index_WithPageBeyondTheLastPage_ClampsToTheLastPage()
    {
        // Arrange
        await this.GivenAuthenticatedUserAsync();
        var catId = await this.SeedCategoryAsync("Clamp High");
        await this.SeedPostAsync("Only Post", catId);
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync("/blog?page=999", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("Only Post");
    }

    [Fact]
    public async Task Index_SidebarCounts_ExcludeDraftsAndTrashedPosts()
    {
        // Post's soft-delete filter hides trashed posts from c.Posts.Count, but nothing hid
        // drafts - and PostTag.IsDeleted is never written, so tag counts included both.

        // Arrange
        await this.GivenAdminUserAsync();
        var catId = await this.SeedCategoryAsync("Counted");
        var tagId = await this.SeedTagAsync("counted-tag");

        await this.SeedPostAsync("Published Counted", catId, isPublished: true, tagIds: [tagId]);
        await this.SeedPostAsync("Draft Not Counted", catId, isPublished: false, tagIds: [tagId]);
        var (trashedId, _) = await this.SeedPostAsync("Trashed Not Counted", catId, isPublished: true, tagIds: [tagId]);

        var page = await HttpClient.GetAsync("/Admin/Post", TestContext.Current.CancellationToken);
        var token = await GetCsrfToken(page);
        await HttpClient.PostAsync($"/Admin/Post/SoftDelete/{trashedId}",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }),
            TestContext.Current.CancellationToken);

        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync("/blog", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var document = await response.ParseHtmlAsync();

        // Three posts reference the category and the tag; only one is publicly visible.
        var categoryCounts = document.QuerySelectorAll("#categoryFilter .chip-count")
            .Select(e => e.TextContent.Trim())
            .ToArray();
        categoryCounts.ShouldBe(["1"]);

        var tagCounts = document.QuerySelectorAll("#tagFilter .chip-count")
            .Select(e => e.TextContent.Trim())
            .ToArray();
        tagCounts.ShouldBe(["1"]);
    }

    [Fact]
    public async Task Index_SidebarOmitsCategoriesAndTagsWithNoVisiblePosts()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var usedCategoryId = await this.SeedCategoryAsync("Used");
        await this.SeedCategoryAsync("Empty");
        var usedTagId = await this.SeedTagAsync("used-tag");
        await this.SeedTagAsync("empty-tag");

        await this.SeedPostAsync("Visible Post", usedCategoryId, isPublished: true, tagIds: [usedTagId]);
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync("/blog", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var document = await response.ParseHtmlAsync();

        var categoryNames = document.QuerySelectorAll("#categoryFilter .filter-chip span:not(.chip-count)")
            .Select(e => e.TextContent.Trim())
            .ToArray();
        categoryNames.ShouldContain("Used");
        categoryNames.ShouldNotContain("Empty");

        var tagNames = document.QuerySelectorAll("#tagFilter .filter-chip span:not(.chip-count)")
            .Select(e => e.TextContent.Trim())
            .ToArray();
        tagNames.ShouldContain("used-tag");
        tagNames.ShouldNotContain("empty-tag");
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
        // Act: filters are keyed on slugs, so renaming a category cannot break saved links.
        var matchingResponse = await HttpClient.GetAsync("/blog?category=tech&tag=dotnet", TestContext.Current.CancellationToken);
        var wrongTagResponse = await HttpClient.GetAsync("/blog?tag=cooking", TestContext.Current.CancellationToken);
        var otherCategoryResponse = await HttpClient.GetAsync("/blog?category=life", TestContext.Current.CancellationToken);

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

    [Fact]
    public async Task Post_UnknownSlug_RendersTheErrorPageNotABlankBody()
    {
        // There was no UseStatusCodePages, so a bad slug returned 404 with an empty body.

        // Arrange
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync("/blog/no-such-post", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(NotFound);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldNotBeNullOrWhiteSpace();
        content.ShouldContain("Page not found");
        content.ShouldNotContain("\"error\":");
    }

    [Fact]
    public async Task Post_DraftSlug_IsNotPubliclyReachable()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var catId = await this.SeedCategoryAsync("Hidden");
        var (_, slug) = await this.SeedPostAsync("Secret Draft", catId, isPublished: false);
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync($"/blog/{slug}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(NotFound);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldNotContain("Secret Draft");
    }

    [Fact]
    public async Task Register_IsClosedByDefault()
    {
        // A single-author blog should not expose an open account-creation endpoint.

        // Arrange
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync("/Identity/Account/Register", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldNotBe(OK);
    }

    [Fact]
    public async Task Index_DoesNotLinkToRegistration()
    {
        // Arrange
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync("/blog", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var document = await response.ParseHtmlAsync();
        document.QuerySelectorAll("a")
            .Select(a => a.GetAttribute("href") ?? string.Empty)
            .ShouldNotContain(href => href.Contains("Account/Register"));
    }
}