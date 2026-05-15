using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests.Api.Admin;

public class PostTests(IntegrationTestFixture fixture)
    : BaseIntegrationTest(fixture)
{
    [Fact]
    public async Task Index_WhenAdmin_ReturnsOk()
    {
        // Arrange
        await this.GivenAdminUserAsync();

        // Act
        var response = await HttpClient.GetAsync("/Admin/Post", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
    }

    [Fact]
    public async Task GetPostsData_WithMalformedPagingAndOrdering_DoesNotFail()
    {
        // Arrange
        await this.GivenAdminUserAsync();

        var createPageResponse = await HttpClient.GetAsync("/Admin/Post/Create", TestContext.Current.CancellationToken);
        var csrfToken = await GetCsrfToken(createPageResponse);

        var formData = new Dictionary<string, string>
        {
            ["draw"] = "not-a-number",
            ["start"] = "-50",
            ["length"] = "100000",
            ["search[regex]"] = "not-a-bool",
            ["columns[0][data]"] = "title",
            ["columns[0][name]"] = "Title",
            ["columns[0][orderable]"] = "true",
            ["columns[0][searchable]"] = "true",
            ["columns[0][search][regex]"] = "false",
            ["order[0][column]"] = "999",
            ["order[0][dir]"] = "sideways",
            ["__RequestVerificationToken"] = csrfToken
        };

        // Act
        var response = await HttpClient.PostAsync(
            "/Admin/Post/GetPostsData",
            new FormUrlEncodedContent(formData),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var payload = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        payload.ShouldContain("recordsTotal");
    }

    [Fact]
    public async Task Create_Post_WhenAdmin_RedirectsToIndex()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var categoryId = await this.SeedCategoryAsync("Integration Test Category");

        var createPageResponse = await HttpClient.GetAsync("/Admin/Post/Create", TestContext.Current.CancellationToken);
        var csrfToken = await GetCsrfToken(createPageResponse);

        var createModel = new Dictionary<string, string>
        {
            ["Title"] = "Integration Test Post",
            ["Content"] = "This is a test content",
            ["CategoryId"] = categoryId.ToString(),
            ["PublishNow"] = "true",
            ["__RequestVerificationToken"] = csrfToken
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

        var createPageResponse = await HttpClient.GetAsync("/Admin/Post/Create", TestContext.Current.CancellationToken);
        var csrfToken = await GetCsrfToken(createPageResponse);

        var longTitle = new string('A', 256);
        var createModel = new Dictionary<string, string>
        {
            ["Title"] = longTitle,
            ["Content"] = "Valid content",
            ["CategoryId"] = categoryId.ToString(),
            ["PublishNow"] = "true",
            ["__RequestVerificationToken"] = csrfToken
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

        // Act
        var response = await HttpClient.GetAsync($"/blog/{slug}", TestContext.Current.CancellationToken);

        // Assert
        slug.ShouldStartWith(expectedSlugPattern);
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

        var editPageResponse = await HttpClient.GetAsync($"/Admin/Post/Edit/{postId}", TestContext.Current.CancellationToken);
        var csrfToken = await GetCsrfToken(editPageResponse);

        var editModel1 = new Dictionary<string, string>
        {
            ["Id"] = postId.ToString(),
            ["Title"] = "First Edit",
            ["Content"] = "First content",
            ["PublishNow"] = "true",
            ["__RequestVerificationToken"] = csrfToken
        };

        var editModel2 = new Dictionary<string, string>
        {
            ["Id"] = postId.ToString(),
            ["Title"] = "Second Edit",
            ["Content"] = "Second content",
            ["PublishNow"] = "true",
            ["__RequestVerificationToken"] = csrfToken
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
        post.Title.ShouldBeOneOf("First Edit", "Second Edit");
    }

    [Fact]
    public async Task Publish_Post_WhenAdmin_ReturnsSuccess()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var catId = await this.SeedCategoryAsync("Publish Test");
        var (postId, _) = await this.SeedPostAsync("Draft for Publish", catId, isPublished: false);

        var createPageResponse = await HttpClient.GetAsync("/Admin/Post/Create", TestContext.Current.CancellationToken);
        var csrfToken = await GetCsrfToken(createPageResponse);

        // Act
        var response = await HttpClient.PostAsync(
            $"/Admin/Post/Publish/{postId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = csrfToken
            }),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().ShouldBeTrue();

        var post = await ExecuteDbContextAsync(async db => await db.Posts.FindAsync(postId));
        post!.IsPublished.ShouldBeTrue();
    }

    [Fact]
    public async Task Unpublish_Post_WhenAdmin_ReturnsSuccess()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var catId = await this.SeedCategoryAsync("Unpublish Test");
        var (postId, _) = await this.SeedPostAsync("Published for Unpublish", catId, isPublished: true);

        var createPageResponse = await HttpClient.GetAsync("/Admin/Post/Create", TestContext.Current.CancellationToken);
        var csrfToken = await GetCsrfToken(createPageResponse);

        // Act
        var response = await HttpClient.PostAsync(
            $"/Admin/Post/Unpublish/{postId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = csrfToken
            }),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().ShouldBeTrue();

        var post = await ExecuteDbContextAsync(async db => await db.Posts.FindAsync(postId));
        post!.IsPublished.ShouldBeFalse();
    }

    [Fact]
    public async Task SoftDelete_Post_WhenAdmin_ReturnsSuccess()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var catId = await this.SeedCategoryAsync("SoftDelete Test");
        var (postId, _) = await this.SeedPostAsync("Post to Trash", catId);

        var createPageResponse = await HttpClient.GetAsync("/Admin/Post/Create", TestContext.Current.CancellationToken);
        var csrfToken = await GetCsrfToken(createPageResponse);

        // Act
        var response = await HttpClient.PostAsync(
            $"/Admin/Post/SoftDelete/{postId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = csrfToken
            }),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().ShouldBeTrue();

        var post = await ExecuteDbContextAsync(async db =>
            await db.Posts.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == postId));
        post!.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task Restore_TrashedPost_WhenAdmin_ReturnsSuccess()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var catId = await this.SeedCategoryAsync("Restore Test");
        var (postId, _) = await this.SeedPostAsync("Trashed Post", catId);

        // soft-delete via DB directly so we can then test Restore
        await ExecuteDbContextAsync(async db =>
        {
            var p = await db.Posts.FindAsync(postId);
            p!.IsDeleted = true;
            await db.SaveChangesAsync();
        });

        var createPageResponse = await HttpClient.GetAsync("/Admin/Post/Create", TestContext.Current.CancellationToken);
        var csrfToken = await GetCsrfToken(createPageResponse);

        // Act
        var response = await HttpClient.PostAsync(
            $"/Admin/Post/Restore/{postId}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = csrfToken
            }),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("success").GetBoolean().ShouldBeTrue();

        var post = await ExecuteDbContextAsync(async db =>
            await db.Posts.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == postId));
        post!.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task DataTables_RecordsTotal_CountsAllForStatus_WhileFilteredCountIsNarrowed()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var catId = await this.SeedCategoryAsync("Count Test");
        await this.SeedPostAsync("Alpha Post", catId);
        await this.SeedPostAsync("Beta Post", catId);
        await this.SeedPostAsync("Gamma Post", catId);

        var createPageResponse = await HttpClient.GetAsync("/Admin/Post/Create", TestContext.Current.CancellationToken);
        var csrfToken = await GetCsrfToken(createPageResponse);

        var formData = new Dictionary<string, string>
        {
            ["draw"] = "1",
            ["start"] = "0",
            ["length"] = "10",
            ["search[value]"] = "Alpha",
            ["search[regex]"] = "false",
            ["statusFilter"] = "Active",
            ["columns[0][data]"] = "title",
            ["columns[0][name]"] = "Title",
            ["columns[0][orderable]"] = "true",
            ["columns[0][searchable]"] = "true",
            ["columns[0][search][value]"] = "",
            ["columns[0][search][regex]"] = "false",
            ["order[0][column]"] = "0",
            ["order[0][dir]"] = "asc",
            ["__RequestVerificationToken"] = csrfToken
        };

        // Act
        var response = await HttpClient.PostAsync(
            "/Admin/Post/GetPostsData",
            new FormUrlEncodedContent(formData),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var payload = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        root.GetProperty("recordsTotal").GetInt32().ShouldBe(3);   // all active posts
        root.GetProperty("recordsFiltered").GetInt32().ShouldBe(1); // only "Alpha Post" matches
    }

    [Fact]
    public async Task Preview_DraftPost_WhenAdmin_ReturnsOk()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var catId = await this.SeedCategoryAsync("Preview Test");
        var (postId, _) = await this.SeedPostAsync("Draft Preview Post", catId, isPublished: false);

        // Act
        var response = await HttpClient.GetAsync(
            $"/Admin/Post/Preview/{postId}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
    }
}