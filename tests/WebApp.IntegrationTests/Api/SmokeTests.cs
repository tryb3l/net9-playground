using System.Threading.Tasks;
using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests.Infrastructure;

public class SmokeTests(IntegrationTestFixture fixture)
    : BaseIntegrationTest(fixture)
{
    [Theory]
    [InlineData("/favicon.ico")]
    public async Task StaticAssets_AreServed(string url)
    {
        // Arrange
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBeOneOf(OK, NotModified);
    }

    [Fact]
    public async Task Database_CanConnect()
    {
        // Act
        var canConnect = await ExecuteDbContextAsync(db => 
            db.Database.CanConnectAsync(TestContext.Current.CancellationToken));

        // Assert
        canConnect.ShouldBeTrue();
    }
    
    [Theory]
    [InlineData("/")]     
    [InlineData("/Blog")] 
    [InlineData("/About")]
    public async Task PublicPages_ReturnOk(string url)
    {
        // Arrange
        this.GivenAnonymousUser();
        
        // Act
        var response = await HttpClient.GetAsync(url, TestContext.Current.CancellationToken);
    
        // Assert
        response.StatusCode.ShouldBe(OK);
        (response.Content.Headers.ContentLength ?? 0).ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task PublicPages_IncludeBaselineSecurityHeaders()
    {
        // Arrange
        this.GivenAnonymousUser();

        // Act
        var response = await HttpClient.GetAsync("/", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(OK);
        response.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        response.Headers.GetValues("X-Frame-Options").ShouldContain("DENY");
        response.Headers.GetValues("Referrer-Policy").ShouldContain("strict-origin-when-cross-origin");
    }
}