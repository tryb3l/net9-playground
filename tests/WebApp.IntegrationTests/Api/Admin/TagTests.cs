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

public class TagTests(IntegrationTestFixture fixture)
    : BaseIntegrationTest(fixture)
{
    [Fact]
    public async Task Create_ValidTag_RedirectsToIndex()
    {
        // Arrange
        await this.GivenAdminUserAsync();

        var createPage = await HttpClient.GetAsync("/Admin/Tag/Create", TestContext.Current.CancellationToken);
        var token = await GetCsrfToken(createPage);

        var formData = new Dictionary<string, string>
        {
            ["Name"] = "Security",
            ["__RequestVerificationToken"] = token
        };

        // Act
        var response = await HttpClient.PostAsync(
            "/Admin/Tag/Create",
            new FormUrlEncodedContent(formData),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBeOneOf(Redirect, SeeOther, MovedPermanently);

        var exists = await ExecuteDbContextAsync(db =>
            db.Tags.AnyAsync(t => t.Name == "Security", TestContext.Current.CancellationToken));
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task Create_MissingAntiforgeryToken_IsRejected()
    {
        // Arrange
        await this.GivenAdminUserAsync();

        var formData = new Dictionary<string, string>
        {
            ["Name"] = "NoToken"
        };

        // Act
        var response = await HttpClient.PostAsync(
            "/Admin/Tag/Create",
            new FormUrlEncodedContent(formData),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(BadRequest);

        var exists = await ExecuteDbContextAsync(db =>
            db.Tags.AnyAsync(t => t.Name == "NoToken", TestContext.Current.CancellationToken));
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task QuickCreate_NewName_CreatesTheTagAndReturnsItsId()
    {
        // Adding a tag must not require leaving the post editor and losing the draft.

        // Arrange
        await this.GivenAdminUserAsync();
        var editorPage = await HttpClient.GetAsync("/Admin/Post/Create", TestContext.Current.CancellationToken);
        var token = await GetCsrfToken(editorPage);

        // Act
        var response = await HttpClient.PostAsync("/Admin/Tag/QuickCreate",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["name"] = "  Freshly Typed  ",
                ["__RequestVerificationToken"] = token
            }),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("\"success\":true");
        body.ShouldContain("\"created\":true");
        body.ShouldContain("Freshly Typed");

        var tag = await ExecuteDbContextAsync(db =>
            db.Tags.FirstOrDefaultAsync(t => t.Name == "Freshly Typed", TestContext.Current.CancellationToken));
        tag.ShouldNotBeNull();
    }

    [Fact]
    public async Task QuickCreate_ExistingNameInAnotherCase_ReusesTheTag()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var existingId = await this.SeedTagAsync("dotnet");

        var editorPage = await HttpClient.GetAsync("/Admin/Post/Create", TestContext.Current.CancellationToken);
        var token = await GetCsrfToken(editorPage);

        // Act
        var response = await HttpClient.PostAsync("/Admin/Tag/QuickCreate",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["name"] = "DotNet",
                ["__RequestVerificationToken"] = token
            }),
            TestContext.Current.CancellationToken);

        // Assert
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("\"success\":true");
        body.ShouldContain("\"created\":false");
        body.ShouldContain($"\"id\":{existingId}");

        var count = await ExecuteDbContextAsync(db =>
            db.Tags.CountAsync(t => t.Name.ToLower() == "dotnet", TestContext.Current.CancellationToken));
        count.ShouldBe(1);
    }

    [Fact]
    public async Task QuickCreate_BlankName_IsRejected()
    {
        // Arrange
        await this.GivenAdminUserAsync();
        var editorPage = await HttpClient.GetAsync("/Admin/Post/Create", TestContext.Current.CancellationToken);
        var token = await GetCsrfToken(editorPage);

        // Act
        var response = await HttpClient.PostAsync("/Admin/Tag/QuickCreate",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["name"] = "   ",
                ["__RequestVerificationToken"] = token
            }),
            TestContext.Current.CancellationToken);

        // Assert
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldContain("\"success\":false");
    }

    [Fact]
    public async Task QuickCreate_WithoutAdminRole_IsRejected()
    {
        // Arrange
        await this.GivenAuthenticatedUserAsync();

        // Act
        var response = await HttpClient.PostAsync("/Admin/Tag/QuickCreate",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["name"] = "sneaky" }),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldNotBe(OK);

        var exists = await ExecuteDbContextAsync(db =>
            db.Tags.AnyAsync(t => t.Name == "sneaky", TestContext.Current.CancellationToken));
        exists.ShouldBeFalse();
    }
}

