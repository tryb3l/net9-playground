using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests.Api.Admin;

public class CategoryTests(IntegrationTestFixture fixture)
    : BaseIntegrationTest(fixture)
{
    [Fact]
    public async Task Index_ReturnsOk_ForAdmin()
    {
        // Arrange
        await this.GivenAdminUserAsync();

        // Act
        var response = await HttpClient.GetAsync("/Admin/Category", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
    }

    [Fact]
    public async Task Create_ValidCategory_RedirectsToIndex()
    {
        // Arrange
        await this.GivenAdminUserAsync();

        var createPage = await HttpClient.GetAsync("/Admin/Category/Create", TestContext.Current.CancellationToken);
        var token = await GetCsrfToken(createPage);

        var formData = new Dictionary<string, string>
        {
            ["Name"] = "New Category",
            ["__RequestVerificationToken"] = token
        };

        // Act
        var response = await HttpClient.PostAsync("/Admin/Category/Create", new FormUrlEncodedContent(formData), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBeOneOf(Redirect, SeeOther, MovedPermanently);

        // Assert persisted state
        var exists = await ExecuteDbContextAsync(async db =>
            await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Categories, c => c.Name == "New Category"));

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task Create_InvalidCategory_ReturnsViewWithErrors()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        
        var createPage = await HttpClient.GetAsync("/Admin/Category/Create", TestContext.Current.CancellationToken);
        var token = await GetCsrfToken(createPage);

        var formData = new Dictionary<string, string> 
        { 
            ["Name"] = "",
            ["__RequestVerificationToken"] = token
        };

        // Act
        var response = await HttpClient.PostAsync("/Admin/Category/Create", new FormUrlEncodedContent(formData), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("Category name is required");
    }

    [Fact]
    public async Task Delete_CategoryWithPosts_SucceedsAndUncategorizesThem()
    {
        // FK_Posts_Categories_CategoryId is ON DELETE NO ACTION, so dependent posts must be
        // detached explicitly or Postgres rejects the delete.

        // Arrange
        await this.GivenAdminUserAsync();
        var categoryId = await this.SeedCategoryAsync("Doomed");
        var (publishedPostId, _) = await this.SeedPostAsync("Live Post", categoryId);
        var (trashedPostId, _) = await this.SeedPostAsync("Trashed Post", categoryId);

        var page = await HttpClient.GetAsync("/Admin/Category", TestContext.Current.CancellationToken);
        var token = await GetCsrfToken(page);

        // Trashed posts are hidden by the global query filter but still hold the FK.
        await HttpClient.PostAsync($"/Admin/Post/SoftDelete/{trashedPostId}",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }),
            TestContext.Current.CancellationToken);

        // Act
        var response = await HttpClient.PostAsync($"/Admin/Category/Delete/{categoryId}",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBeOneOf(OK, Redirect, SeeOther, MovedPermanently);

        await ExecuteDbContextAsync(async db =>
        {
            (await db.Categories.FindAsync(categoryId)).ShouldBeNull();

            var live = await db.Posts.FindAsync(publishedPostId);
            live.ShouldNotBeNull();
            live.CategoryId.ShouldBeNull();

            var trashed = await db.Posts.IgnoreQueryFilters().FirstAsync(p => p.Id == trashedPostId);
            trashed.CategoryId.ShouldBeNull();
        });
    }

    [Fact]
    public async Task Create_WhenNameNormalizesToAnExistingSlug_StillSucceeds()
    {
        // IX_Categories_Slug is unique; "My Blog" and "My  Blog!" both slug to "my-blog".

        // Arrange
        await this.GivenAdminUserAsync();
        await this.SeedCategoryAsync("My Blog");

        var createPage = await HttpClient.GetAsync("/Admin/Category/Create", TestContext.Current.CancellationToken);
        var token = await GetCsrfToken(createPage);

        var formData = new Dictionary<string, string>
        {
            ["Name"] = "My  Blog!",
            ["__RequestVerificationToken"] = token
        };

        // Act
        var response = await HttpClient.PostAsync("/Admin/Category/Create", new FormUrlEncodedContent(formData), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBeOneOf(Redirect, SeeOther, MovedPermanently);

        await ExecuteDbContextAsync(async db =>
        {
            var created = await db.Categories.FirstOrDefaultAsync(c => c.Name == "My  Blog!");
            created.ShouldNotBeNull();
            created.Slug.ShouldBe("my-blog-1");
        });
    }
}