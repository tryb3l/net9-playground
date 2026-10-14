using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WebApp.IntegrationTests.Fixtures;
using WebApp.IntegrationTests.Support;
using WebApp.IntegrationTests.Support.Extensions;
using static System.Net.HttpStatusCode;

namespace WebApp.IntegrationTests.Api.Admin;

public class AttachmentUploadTests(IntegrationTestFixture fixture) : BaseIntegrationTest(fixture)
{
    private readonly List<string> _uploadedUrls = [];

    public override async ValueTask DisposeAsync()
    {
        var webRoot = Services.GetRequiredService<IWebHostEnvironment>().WebRootPath;
        foreach (var url in _uploadedUrls)
        {
            var path = Path.Combine(webRoot, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path)) File.Delete(path);
        }

        await base.DisposeAsync();
    }

    private async Task<HttpResponseMessage> PostImageAsync(string endpoint, string fieldName, byte[] bytes,
        string fileName, string contentType)
    {
        await this.GivenAdminUserAsync();
        var page = await HttpClient.GetAsync("/Admin/Tag/Create", TestContext.Current.CancellationToken);
        var token = await GetCsrfToken(page);

        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent
        {
            { fileContent, fieldName, fileName },
            { new StringContent(token), "__RequestVerificationToken" }
        };

        return await HttpClient.PostAsync(endpoint, form, TestContext.Current.CancellationToken);
    }

    private static byte[] Image(int width, int height, string suffix)
    {
        using var image = NetVips.Image.Black(width, height, bands: 3);
        return image.WriteToBuffer(suffix);
    }

    private async Task AssertServedAsWebpAsync(string url)
    {
        _uploadedUrls.Add(url);
        var served = await HttpClient.GetAsync(url, TestContext.Current.CancellationToken);
        served.StatusCode.ShouldBe(OK);
        served.Content.Headers.ContentType!.MediaType.ShouldBe("image/webp");
    }

    [Fact]
    public async Task Upload_ValidJpeg_ReturnsAllVariantsServedAsWebp()
    {
        var response = await PostImageAsync("/Admin/api/Attachments/upload", "filepond", Image(1000, 500, ".jpg"),
            "photo.jpg", "image/jpeg");

        response.StatusCode.ShouldBe(OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var urls = json.GetProperty("urls");
        var variantUrls = new[] { "large", "medium", "thumbnail" }.Select(n => urls.GetProperty(n).GetString()!).ToList();
        _uploadedUrls.AddRange(variantUrls);
        foreach (var url in variantUrls)
            await AssertServedAsWebpAsync(url);
    }

    [Fact]
    public async Task Upload_TiffDisguisedAsJpeg_ReturnsBadRequest()
    {
        var response = await PostImageAsync("/Admin/api/Attachments/upload", "filepond", Image(20, 20, ".tif"),
            "photo.jpg", "image/jpeg");

        response.StatusCode.ShouldBe(BadRequest);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldContain("not a valid image");
    }

    [Fact]
    public async Task EditorUpload_ValidJpeg_ReturnsSuccessAndServesWebp()
    {
        var response = await PostImageAsync("/Admin/api/Attachments/editor-upload", "image", Image(1000, 500, ".jpg"),
            "inline.jpg", "image/jpeg");

        response.StatusCode.ShouldBe(OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        json.GetProperty("success").GetInt32().ShouldBe(1);
        await AssertServedAsWebpAsync(json.GetProperty("file").GetProperty("url").GetString()!);
    }
}
