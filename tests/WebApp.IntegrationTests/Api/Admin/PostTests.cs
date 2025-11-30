using Microsoft.EntityFrameworkCore;
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
    
    [Fact]
    public async Task Create_Post_WithTitleOver255Chars_ReturnsValidationError()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var categoryId = await this.SeedCategoryAsync("Validation Test");

        var longTitle = new string('A', 256);
        var createModel = new Dictionary<string, string>
        {
            ["Title"] = longTitle,
            ["Content"] = "Valid content",
            ["CategoryId"] = categoryId.ToString(),
            ["PublishNow"] = "true"
        };

        var content = new FormUrlEncodedContent(createModel);

        // Act
        var response = await HttpClient.PostAsync("/Admin/Post/Create", content, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        
        var document = await response.ParseHtmlAsync();
        var validationErrors = document.QuerySelectorAll(".validation-summary-errors, .text-danger, .field-validation-error");
        validationErrors.Length.ShouldBeGreaterThan(0);
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

        // Assert
        slug.ShouldStartWith(expectedSlugPattern);

        // Act
        var response = await HttpClient.GetAsync($"/blog/{slug}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var document = await response.ParseHtmlAsync();
        var titleElement = document.QuerySelector("h1, .post-full-title");
        titleElement?.TextContent.ShouldContain(title);
    }
    
    [Fact]
    public async Task Edit_Post_ConcurrentUpdates_LastWriteWins()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var catId = await this.SeedCategoryAsync("Concurrency Test");
        var (postId, _) = await this.SeedPostAsync("Original Title", catId);

        var editModel1 = new Dictionary<string, string>
        {
            ["Id"] = postId.ToString(),
            ["Title"] = "First Edit",
            ["Content"] = "First content",
            ["PublishNow"] = "true"
        };

        var editModel2 = new Dictionary<string, string>
        {
            ["Id"] = postId.ToString(),
            ["Title"] = "Second Edit",
            ["Content"] = "Second content",
            ["PublishNow"] = "true"
        };

        // Act
        var task1 = HttpClient.PostAsync($"/Admin/Post/Edit/{postId}", 
            new FormUrlEncodedContent(editModel1), TestContext.Current.CancellationToken);
        var task2 = HttpClient.PostAsync($"/Admin/Post/Edit/{postId}", 
            new FormUrlEncodedContent(editModel2), TestContext.Current.CancellationToken);

        await Task.WhenAll(task1, task2);

        // Assert
        var post = await ExecuteDbContextAsync(async db => 
            await db.Posts.FirstOrDefaultAsync(p => p.Id == postId));

        post.ShouldNotBeNull();
        post!.Title.ShouldBeOneOf("First Edit", "Second Edit");
    }
}