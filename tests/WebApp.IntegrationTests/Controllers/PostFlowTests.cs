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
}