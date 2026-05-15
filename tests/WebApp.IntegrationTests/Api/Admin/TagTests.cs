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
}

