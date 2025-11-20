using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests.Infrastructure;

public class SmokeTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    : BaseIntegrationTest(fixture, output)
{
    [Theory]
    [InlineData("/favicon.ico")]
    public async Task StaticAssets_AreServed(string url)
    {
        var res = await HttpClient.GetAsync(url, TestContext.Current.CancellationToken);
        res.StatusCode.ShouldBeOneOf(OK, NotModified);
    }

    [Fact]
    public async Task Database_CanConnect()
    {
        var canConnect = await ExecuteDbContextAsync(db => 
            db.Database.CanConnectAsync(TestContext.Current.CancellationToken));
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
        var res = await HttpClient.GetAsync(url, TestContext.Current.CancellationToken);
    
        // Assert
        res.StatusCode.ShouldBe(OK);
        (res.Content.Headers.ContentLength ?? 0).ShouldBeGreaterThan(0);
    }
}