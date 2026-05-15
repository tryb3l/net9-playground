using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests.Api.Admin;

public class AdminSecurityTests(IntegrationTestFixture fixture)
    : BaseIntegrationTest(fixture)
{
    [Theory]
    [InlineData("/Admin/Post")]
    [InlineData("/Admin/Post/Create")]
    [InlineData("/Admin/Dashboard")]
    public async Task ProtectedRoutes_ReturnUnauthorized_WhenAnonymous(string url)
    {
        // Arrange
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldNotBe(OK);
    }

    [Theory]
    [InlineData("/Admin/Post")]
    [InlineData("/Admin/Dashboard")]
    public async Task ProtectedRoutes_ReturnForbidden_WhenUserIsNotAdmin(string url)
    {
        // Arrange
        await this.GivenAuthenticatedUserAsync();

        // Act
        var response = await HttpClient.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(Forbidden);
    }
    
    [Fact]
    public async Task AdminRoutes_ReturnOk_WhenUserIsAdmin()
    {
        // Arrange
        await this.GivenAdminUserAsync();

        // Act
        var response = await HttpClient.GetAsync("/Admin/Dashboard", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
    }

    [Fact]
    public async Task CreateTag_WithoutAntiforgeryToken_ReturnsError()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var body = new StringContent("""{"name":"test-tag"}""", Encoding.UTF8, "application/json");
        // Deliberately omit the RequestVerificationToken header

        // Act
        var response = await HttpClient.PostAsync(
            "/Admin/Post/api/tags", body, TestContext.Current.CancellationToken);

        // Assert — [ValidateAntiForgeryToken] must reject the request
        response.StatusCode.ShouldNotBe(OK);
    }

    [Fact]
    public async Task Preview_DraftPost_WhenAnonymous_IsRedirected()
    {
        // Arrange — seed a draft as admin first, then switch to anonymous
        await this.GivenAdminUserAsync();
        var catId = await this.SeedCategoryAsync("Anon Preview Test");
        var (postId, _) = await this.SeedPostAsync("Anon Draft", catId, isPublished: false);
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync(
            $"/Admin/Post/Preview/{postId}", TestContext.Current.CancellationToken);

        // Assert — unauthenticated users must not see admin preview
        response.StatusCode.ShouldNotBe(OK);
    }

    [Fact]
    public async Task Preview_DraftPost_WhenNonAdmin_ReturnsForbidden()
    {
        // Arrange — seed a draft as admin, then switch to a regular user
        await this.GivenAdminUserAsync();
        var catId = await this.SeedCategoryAsync("NonAdmin Preview Test");
        var (postId, _) = await this.SeedPostAsync("NonAdmin Draft", catId, isPublished: false);
        await this.GivenAuthenticatedUserAsync(); // non-admin role

        // Act
        var response = await HttpClient.GetAsync(
            $"/Admin/Post/Preview/{postId}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(Forbidden);
    }
}