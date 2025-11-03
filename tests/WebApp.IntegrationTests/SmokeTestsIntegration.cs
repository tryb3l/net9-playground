using System.Net;
using Shouldly;
using Xunit;

namespace WebApp.IntegrationTests;

public class SmokeTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    : BaseIntegrationTest(fixture, output)
{
    [Theory]
    [InlineData("/")]
    [InlineData("/Home/Index")]
    public async Task PublicPages_ReturnOk(string url)
    {
        // Arrange/Act
        var res = await HttpClient.GetAsync(url, TestContext.Current.CancellationToken);
        
        // Assert
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        (res.Content.Headers.ContentLength ?? 1).ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData("/favicon.ico")]
    [InlineData("/css/site.css")]
    public async Task StaticAssets_AreServed(string url)
    {
        // Arrange/Act
        var res = await HttpClient.GetAsync(url, TestContext.Current.CancellationToken);
        
        // Assert
        res.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotModified, HttpStatusCode.Moved, HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task NotFound_Page_Returns404()
    {
        // Arrange/Act
        var res = await HttpClient.GetAsync("/definitely-not-existing-page", TestContext.Current.CancellationToken);
        
        // Assert
        res.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Database_CanConnect()
    {
        // Arrange/Act
        var canConnect = await ExecuteDbContextAsync(db => db.Database.CanConnectAsync(TestContext.Current.CancellationToken));
        
        // Assert       
        canConnect.ShouldBeTrue();
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/healthz")]
    public async Task HealthEndpoints_Optional_DoNotFailSuite_WhenAbsent(string url)
    {
        // Arrange/Act
        var res = await HttpClient.GetAsync(url, TestContext.Current.CancellationToken);

        // Assert
        if (res.StatusCode == HttpStatusCode.NotFound)
        {
            var body = await res.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            TestContext.Current.AddAttachment($"health_{url.Trim('/').Replace('/', '_')}_404.html", body);
            return;
        }
        
        res.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.Found, HttpStatusCode.TemporaryRedirect);
    }

    [Fact]
    public async Task RobotsTxt_Or_404()
    {
        // Arrange/Act
        var res = await HttpClient.GetAsync("/robots.txt", TestContext.Current.CancellationToken);
        
        // Assert
        res.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SitemapXml_Or_404()
    {
        // Arrange/Act
        var res = await HttpClient.GetAsync("/sitemap.xml", TestContext.Current.CancellationToken);
        
        // Assert       
        res.StatusCode.ShouldBeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }
}