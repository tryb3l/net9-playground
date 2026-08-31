using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;

namespace WebApp.IntegrationTests.Controllers;

[Collection("Integration Tests")]
public class PostFlowTests : BaseIntegrationTest
{
    public PostFlowTests(IntegrationTestFixture fixture) : base(fixture)
    {
        
    }

    [Fact]
    public async Task Post_Lifecycle_HappyPath()
    {
        // Setup: Authenticate as Admin using the Support infrastructure
        await this.GivenAdminUserAsync();
        
        // GET Create Page
        var createPageResponse = await HttpClient.GetAsync("/Admin/Post/Create", TestContext.Current.CancellationToken);
        createPageResponse.EnsureSuccessStatusCode();
        var csrfToken = await GetCsrfToken(createPageResponse);

        // POST Create
        var postTitle = "Integration Test Post";
        var createFormData = new Dictionary<string, string>
        {
            { "Title", postTitle },
            { "Content", "Some content for the integration test." },
            { "PublishNow", "false" },
            { "__RequestVerificationToken", csrfToken }
        };

        var createResponse = await HttpClient.PostAsync("/Admin/Post/Create", new FormUrlEncodedContent(createFormData), TestContext.Current.CancellationToken);
        
        // Assert
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var redirectUrl = createResponse.Headers.Location?.ToString();
        redirectUrl.ShouldNotBeNull();
        redirectUrl.ShouldContain("/Admin/Post/Edit/");
        
        // Extract ID from URL
        var postId = int.Parse(redirectUrl.Split('/').Last());

        // Assert: Verify DB State
        await ExecuteDbContextAsync(async db =>
        {
            var post = await db.Posts.FindAsync(postId);
            post.ShouldNotBeNull();
            post.Title.ShouldBe(postTitle);
            post.IsPublished.ShouldBeFalse();
            post.IsDeleted.ShouldBeFalse();
        });

        // POST Publish
        // The controller uses [ValidateAntiForgeryToken] so we need the token again
        // In a real browser the token is in the Edit page form
        var editPageResponse = await HttpClient.GetAsync(redirectUrl, TestContext.Current.CancellationToken);
        var editCsrfToken = await GetCsrfToken(editPageResponse);

        var publishFormData = new Dictionary<string, string>
        {
            { "__RequestVerificationToken", editCsrfToken }
        };

        var publishResponse = await HttpClient.PostAsync($"/Admin/Post/Publish/{postId}", new FormUrlEncodedContent(publishFormData), TestContext.Current.CancellationToken);
        publishResponse.EnsureSuccessStatusCode();

        // Assert
        await ExecuteDbContextAsync(async db =>
        {
            var post = await db.Posts.FindAsync(postId);
            post.ShouldNotBeNull();
            post.IsPublished.ShouldBeTrue();
        });

        // POST Soft Delete
        // AJAX call sends the token.
        var softDeleteFormData = new Dictionary<string, string>
        {
            { "__RequestVerificationToken", editCsrfToken }
        };

        var deleteResponse = await HttpClient.PostAsync($"/Admin/Post/SoftDelete/{postId}", new FormUrlEncodedContent(softDeleteFormData), TestContext.Current.CancellationToken);
        deleteResponse.EnsureSuccessStatusCode();

        // Assert
        await ExecuteDbContextAsync(async db =>
        {
            var post = await db.Posts.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == postId);
            post.ShouldNotBeNull();
            post.IsDeleted.ShouldBeTrue();
            
            // Ensure it is NOT hard deleted
            var count = await db.Posts.IgnoreQueryFilters().CountAsync();
            count.ShouldBe(1);
        });
    }
    [Fact]
    public async Task Edit_WithUnchangedTags_PreservesTags()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var categoryId = await this.SeedCategoryAsync("Tech");
        var tagId1 = await this.SeedTagAsync("dotnet");
        var tagId2 = await this.SeedTagAsync("csharp");

        var createPageResponse = await HttpClient.GetAsync("/Admin/Post/Create", TestContext.Current.CancellationToken);
        createPageResponse.EnsureSuccessStatusCode();
        var createToken = await GetCsrfToken(createPageResponse);

        // Act: create a post with two tags
        var createFormData = new List<KeyValuePair<string, string>>
        {
            new("Title", "Post With Tags"),
            new("Content", "Some content for the tag test."),
            new("PublishNow", "false"),
            new("CategoryId", categoryId.ToString()),
            new("SelectedTagIds", tagId1.ToString()),
            new("SelectedTagIds", tagId2.ToString()),
            new("__RequestVerificationToken", createToken)
        };

        var createResponse = await HttpClient.PostAsync("/Admin/Post/Create", new FormUrlEncodedContent(createFormData), TestContext.Current.CancellationToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var redirectUrl = createResponse.Headers.Location?.ToString();
        redirectUrl.ShouldNotBeNull();
        var postId = int.Parse(redirectUrl.Split('/').Last());

        // Act: edit only the title, resubmitting the same tags
        var editPageResponse = await HttpClient.GetAsync(redirectUrl, TestContext.Current.CancellationToken);
        editPageResponse.EnsureSuccessStatusCode();
        var editToken = await GetCsrfToken(editPageResponse);

        var editFormData = new List<KeyValuePair<string, string>>
        {
            new("Id", postId.ToString()),
            new("Title", "Post With Tags Renamed"),
            new("Content", "Some content for the tag test."),
            new("PublishNow", "false"),
            new("CategoryId", categoryId.ToString()),
            new("SelectedTagIds", tagId1.ToString()),
            new("SelectedTagIds", tagId2.ToString()),
            new("__RequestVerificationToken", editToken)
        };

        var editResponse = await HttpClient.PostAsync($"/Admin/Post/Edit/{postId}", new FormUrlEncodedContent(editFormData), TestContext.Current.CancellationToken);

        // Assert
        editResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        await ExecuteDbContextAsync(async db =>
        {
            var post = await db.Posts
                .Include(p => p.PostTags)
                .FirstOrDefaultAsync(p => p.Id == postId);
            post.ShouldNotBeNull();
            post.Title.ShouldBe("Post With Tags Renamed");
            post.Slug.ShouldBe("post-with-tags-renamed");

            var tagIds = post.PostTags.Select(pt => pt.TagId).OrderBy(id => id).ToArray();
            tagIds.ShouldBe([tagId1, tagId2]);
        });
    }

    [Fact]
    public async Task Delete_PermanentDelete_ReturnsJsonSuccess_AndRemovesPost()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var categoryId = await this.SeedCategoryAsync("Tech");
        var tagId = await this.SeedTagAsync("dotnet");
        var (postId, _) = await this.SeedPostAsync("Post To Delete", categoryId, tagIds: [tagId]);

        var createPageResponse = await HttpClient.GetAsync("/Admin/Post/Create", TestContext.Current.CancellationToken);
        createPageResponse.EnsureSuccessStatusCode();
        var csrfToken = await GetCsrfToken(createPageResponse);

        // Act
        var deleteFormData = new Dictionary<string, string>
        {
            { "__RequestVerificationToken", csrfToken }
        };
        var response = await HttpClient.PostAsync($"/Admin/Post/Delete/{postId}", new FormUrlEncodedContent(deleteFormData), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("\"success\":true");

        await ExecuteDbContextAsync(async db =>
        {
            var post = await db.Posts.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == postId);
            post.ShouldBeNull();

            var tagLinks = await db.PostTags.IgnoreQueryFilters()
                .Where(pt => pt.PostId == postId)
                .ToListAsync();
            tagLinks.ShouldBeEmpty();
        });
    }

    [Fact]
    public async Task Create_WhenSlugAlreadyExists_AppendsCounter()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var categoryId = await this.SeedCategoryAsync("Tech");
        await this.SeedPostAsync("Hello World", categoryId);

        var createPageResponse = await HttpClient.GetAsync("/Admin/Post/Create", TestContext.Current.CancellationToken);
        createPageResponse.EnsureSuccessStatusCode();
        var csrfToken = await GetCsrfToken(createPageResponse);

        // Act
        var formData = new Dictionary<string, string>
        {
            { "Title", "Hello World" },
            { "Content", "Another post with the same title." },
            { "PublishNow", "false" },
            { "__RequestVerificationToken", csrfToken }
        };
        var response = await HttpClient.PostAsync("/Admin/Post/Create", new FormUrlEncodedContent(formData), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var postId = int.Parse(response.Headers.Location!.ToString().Split('/').Last());

        await ExecuteDbContextAsync(async db =>
        {
            var post = await db.Posts.FindAsync(postId);
            post.ShouldNotBeNull();
            post.Slug.ShouldBe("hello-world-1");
        });
    }

    [Fact]
    public async Task Edit_PublishedPost_KeepsSlug_WhenTitleChanges()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var categoryId = await this.SeedCategoryAsync("Tech");
        var (postId, slug) = await this.SeedPostAsync("Published Post", categoryId, isPublished: true);

        var (originalCreatedAt, originalAuthorId) = await ExecuteDbContextAsync(async db =>
        {
            var seeded = await db.Posts.AsNoTracking().FirstAsync(p => p.Id == postId);
            return (seeded.CreatedAt, seeded.AuthorId);
        });

        var editPageResponse = await HttpClient.GetAsync($"/Admin/Post/Edit/{postId}", TestContext.Current.CancellationToken);
        editPageResponse.EnsureSuccessStatusCode();
        var csrfToken = await GetCsrfToken(editPageResponse);

        // Act
        var formData = new Dictionary<string, string>
        {
            { "Id", postId.ToString() },
            { "Title", "Published Post Renamed" },
            { "Content", "Content" },
            { "PublishNow", "true" },
            { "__RequestVerificationToken", csrfToken }
        };
        var response = await HttpClient.PostAsync($"/Admin/Post/Edit/{postId}", new FormUrlEncodedContent(formData), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        await ExecuteDbContextAsync(async db =>
        {
            var post = await db.Posts.FindAsync(postId);
            post.ShouldNotBeNull();
            post.Title.ShouldBe("Published Post Renamed");
            post.Slug.ShouldBe(slug);
            post.CreatedAt.ShouldBe(originalCreatedAt);
            post.AuthorId.ShouldBe(originalAuthorId);
        });
    }

    [Fact]
    public async Task Edit_WithSameImageUrl_PreservesStoredJson()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var categoryId = await this.SeedCategoryAsync("Tech");
        const string storedJson = """{"large":"/uploads/large.jpg","thumbnail":"/uploads/thumb.jpg"}""";
        var (postId, _) = await this.SeedPostAsync("Image Post", categoryId, isPublished: false, featuredImageUrls: storedJson);

        var editPageResponse = await HttpClient.GetAsync($"/Admin/Post/Edit/{postId}", TestContext.Current.CancellationToken);
        editPageResponse.EnsureSuccessStatusCode();
        var csrfToken = await GetCsrfToken(editPageResponse);

        // Act: resubmit the same large image URL
        var formData = new Dictionary<string, string>
        {
            { "Id", postId.ToString() },
            { "Title", "Image Post" },
            { "Content", "Content" },
            { "PublishNow", "false" },
            { "FeaturedImageUrl", "/uploads/large.jpg" },
            { "__RequestVerificationToken", csrfToken }
        };
        var response = await HttpClient.PostAsync($"/Admin/Post/Edit/{postId}", new FormUrlEncodedContent(formData), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        await ExecuteDbContextAsync(async db =>
        {
            var post = await db.Posts.FindAsync(postId);
            post.ShouldNotBeNull();
            post.FeaturedImageUrls.ShouldBe(storedJson);
        });
    }

    [Fact]
    public async Task Create_WhenSlugIsHeldByATrashedPost_StillSucceeds()
    {
        // The unique index on Slug does not exclude soft-deleted rows, so uniqueness has to
        // be checked with the global filter off - otherwise this insert fails with 23505.

        // Arrange
        await this.GivenAdminUserAsync();
        var categoryId = await this.SeedCategoryAsync("Tech");
        var (trashedPostId, _) = await this.SeedPostAsync("Recycled Title", categoryId);

        var page = await HttpClient.GetAsync("/Admin/Post/Create", TestContext.Current.CancellationToken);
        page.EnsureSuccessStatusCode();
        var csrfToken = await GetCsrfToken(page);

        var trashResponse = await HttpClient.PostAsync(
            $"/Admin/Post/SoftDelete/{trashedPostId}",
            new FormUrlEncodedContent(new Dictionary<string, string> { { "__RequestVerificationToken", csrfToken } }),
            TestContext.Current.CancellationToken);
        trashResponse.EnsureSuccessStatusCode();

        // Act
        var formData = new Dictionary<string, string>
        {
            { "Title", "Recycled Title" },
            { "Content", "Reusing the title of a trashed post." },
            { "PublishNow", "false" },
            { "__RequestVerificationToken", csrfToken }
        };
        var response = await HttpClient.PostAsync("/Admin/Post/Create", new FormUrlEncodedContent(formData), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var newPostId = int.Parse(response.Headers.Location!.ToString().Split('/').Last());

        await ExecuteDbContextAsync(async db =>
        {
            var newPost = await db.Posts.FindAsync(newPostId);
            newPost.ShouldNotBeNull();
            newPost.Slug.ShouldBe("recycled-title-1");

            var trashed = await db.Posts.IgnoreQueryFilters().FirstAsync(p => p.Id == trashedPostId);
            trashed.IsDeleted.ShouldBeTrue();
            trashed.Slug.ShouldBe("recycled-title");
        });
    }

    [Fact]
    public async Task Create_WithTitleLongerThanTheSlugColumn_TruncatesInsteadOfFailing()
    {
        // Title allows 128 characters but Slug is varchar(64).

        // Arrange
        await this.GivenAdminUserAsync();

        var page = await HttpClient.GetAsync("/Admin/Post/Create", TestContext.Current.CancellationToken);
        page.EnsureSuccessStatusCode();
        var csrfToken = await GetCsrfToken(page);

        var longTitle = string.Join(' ', Enumerable.Repeat("elaborate", 12)); // 118 chars
        longTitle.Length.ShouldBeGreaterThan(64);

        // Act
        var formData = new Dictionary<string, string>
        {
            { "Title", longTitle },
            { "Content", "A post with a very long title." },
            { "PublishNow", "false" },
            { "__RequestVerificationToken", csrfToken }
        };
        var response = await HttpClient.PostAsync("/Admin/Post/Create", new FormUrlEncodedContent(formData), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var postId = int.Parse(response.Headers.Location!.ToString().Split('/').Last());

        await ExecuteDbContextAsync(async db =>
        {
            var post = await db.Posts.FindAsync(postId);
            post.ShouldNotBeNull();
            post.Title.ShouldBe(longTitle);
            post.Slug.ShouldNotBeNullOrEmpty();
            post.Slug!.Length.ShouldBeLessThanOrEqualTo(64);
        });
    }

    [Fact]
    public async Task Edit_WithNewImageUrl_ReplacesStoredJson()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var categoryId = await this.SeedCategoryAsync("Tech");
        const string storedJson = """{"large":"/uploads/old-large.jpg","thumbnail":"/uploads/old-thumb.jpg"}""";
        var (postId, _) = await this.SeedPostAsync("Image Post", categoryId, isPublished: false, featuredImageUrls: storedJson);

        var editPageResponse = await HttpClient.GetAsync($"/Admin/Post/Edit/{postId}", TestContext.Current.CancellationToken);
        editPageResponse.EnsureSuccessStatusCode();
        var csrfToken = await GetCsrfToken(editPageResponse);

        // Act
        var formData = new Dictionary<string, string>
        {
            { "Id", postId.ToString() },
            { "Title", "Image Post" },
            { "Content", "Content" },
            { "PublishNow", "false" },
            { "FeaturedImageUrl", "/uploads/new-image.jpg" },
            { "__RequestVerificationToken", csrfToken }
        };
        var response = await HttpClient.PostAsync($"/Admin/Post/Edit/{postId}", new FormUrlEncodedContent(formData), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        await ExecuteDbContextAsync(async db =>
        {
            var post = await db.Posts.FindAsync(postId);
            post.ShouldNotBeNull();
            post.FeaturedImageUrls.ShouldBe("/uploads/new-image.jpg");
        });
    }
}