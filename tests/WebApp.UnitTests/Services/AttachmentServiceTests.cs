using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using NetVips;
using WebApp.Services;
using Xunit;

namespace WebApp.UnitTests.Services;

public class AttachmentServiceTests : IDisposable
{
    private readonly string _tempWebRoot;
    private readonly AttachmentService _service;

    public AttachmentServiceTests()
    {
        _tempWebRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempWebRoot);

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

        _service = new AttachmentService(env.Object, NullLogger<AttachmentService>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempWebRoot))
            Directory.Delete(_tempWebRoot, recursive: true);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static IFormFile CreateFormFile(byte[] content, string fileName, string contentType)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }

    private static Task<byte[]> CreateValidJpegBytesAsync(int width = 10, int height = 10) =>
        Task.FromResult(CreateImageBytes(width, height, ".jpg"));

    private static byte[] CreateImageBytes(int width, int height, string suffix)
    {
        using var image = Image.Black(width, height, bands: 3);
        return image.WriteToBuffer(suffix);
    }

    // ── invalid extension ────────────────────────────────────────────────────

    [Theory]
    [InlineData("test.exe")]
    [InlineData("test.pdf")]
    [InlineData("test.svg")]
    [InlineData("test")]
    public async Task ProcessAndSaveImageAsync_ShouldRejectInvalidExtension(string fileName)
    {
        // Arrange
        var file = CreateFormFile([0x00, 0x01], fileName, "image/jpeg");

        // Act
        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts");

        // Assert
        urls.ShouldBeNull();
        error.ShouldNotBeNullOrEmpty();
    }

    // ── fake MIME / corrupted bytes ──────────────────────────────────────────

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldRejectFakeImageBytes()
    {
        // Arrange – extension and MIME look valid but the bytes are not an image
        var fakeBytes = Encoding.UTF8.GetBytes("definitely not an image");
        var file = CreateFormFile(fakeBytes, "photo.jpg", "image/jpeg");

        // Act
        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts");

        // Assert
        urls.ShouldBeNull();
        error.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldRejectCorruptedImage()
    {
        // Arrange – starts with valid JPEG magic bytes but is then truncated/corrupted
        var corruptedBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0xAB, 0xCD };
        var file = CreateFormFile(corruptedBytes, "photo.jpg", "image/jpeg");

        // Act
        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts");

        // Assert
        urls.ShouldBeNull();
        error.ShouldNotBeNullOrEmpty();
    }

    // ── oversized dimensions ─────────────────────────────────────────────────

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldRejectOversizedDimensions()
    {
        // Arrange – 8193×1 image exceeds MaxImageWidth of 8192
        var imageBytes = await CreateValidJpegBytesAsync(width: 8193, height: 1);
        var file = CreateFormFile(imageBytes, "wide.jpg", "image/jpeg");

        // Act
        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts");

        // Assert
        urls.ShouldBeNull();
        error.ShouldNotBeNullOrEmpty();
        error.ShouldContain("8193");
    }

    // ── path traversal ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("../../../etc")]
    [InlineData("..\\..\\windows")]
    [InlineData("posts/../../etc")]
    public async Task ProcessAndSaveImageAsync_ShouldRejectPathTraversalSubfolder(string maliciousSubfolder)
    {
        // Arrange
        var imageBytes = await CreateValidJpegBytesAsync();
        var file = CreateFormFile(imageBytes, "photo.jpg", "image/jpeg");

        // Act
        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, maliciousSubfolder);

        // Assert
        urls.ShouldBeNull();
        error.ShouldNotBeNullOrEmpty();
    }

    // ── cleanup on failed save ────────────────────────────────────────────────

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldNotLeaveFilesOnDisk_WhenImageIsInvalid()
    {
        // Arrange – corrupted bytes cause an exception before any files are written
        var fakeBytes = Encoding.UTF8.GetBytes("not an image");
        var file = CreateFormFile(fakeBytes, "photo.jpg", "image/jpeg");
        var uploadDir = Path.Combine(_tempWebRoot, "uploads", "posts");

        // Act
        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts");

        // Assert
        urls.ShouldBeNull();
        error.ShouldNotBeNullOrEmpty();
        // No files should have been written to the upload directory
        if (Directory.Exists(uploadDir))
            Directory.GetFiles(uploadDir).ShouldBeEmpty();
    }

    // ── happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldReturnUrlsForValidFeaturedImage()
    {
        // Arrange
        var imageBytes = await CreateValidJpegBytesAsync();
        var file = CreateFormFile(imageBytes, "photo.jpg", "image/jpeg");

        // Act
        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts");

        // Assert
        error.ShouldBeNull();
        urls.ShouldNotBeNull();
        urls.ShouldContainKey("large");
        urls.ShouldContainKey("medium");
        urls.ShouldContainKey("thumbnail");
    }

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldReturnSingleUrlForEditorUpload()
    {
        // Arrange
        var imageBytes = await CreateValidJpegBytesAsync();
        var file = CreateFormFile(imageBytes, "inline.jpg", "image/jpeg");

        // Act
        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts/editor");

        // Assert
        error.ShouldBeNull();
        urls.ShouldNotBeNull();
        urls.ShouldContainKey("large");
        urls.Count.ShouldBe(1);
    }

    // ── decoding / variants ──────────────────────────────────────────────────

    private string DiskPath(string url) =>
        Path.Combine(_tempWebRoot, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

    private static byte[] CreateNoisyJpegBytes(int width, int height)
    {
        using var noise = Image.Gaussnoise(width, height, sigma: 40, mean: 128).Cast(Enums.BandFormat.Uchar);
        return noise.WriteToBuffer(".jpg");
    }

    private static int PageCount(Image image) =>
        image.Contains("n-pages") ? (int)image.Get("n-pages") : 1;

    private static byte[] CreateAnimatedGifBytes(int width, int height, int frames)
    {
        var pages = Enumerable.Range(0, frames)
            .Select(i => Image.Black(width, height, bands: 3).Linear([1.0], [i * 25.0]).Cast(Enums.BandFormat.Uchar))
            .ToArray();
        using var joined = Image.Arrayjoin(pages, across: 1);
        using var tagged = joined.Mutate(m =>
        {
            m.Set(GValue.GIntType, "page-height", height);
            m.Set(GValue.ArrayIntType, "delay", Enumerable.Repeat(100, frames).ToArray());
        });
        return tagged.WriteToBuffer(".gif");
    }

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldRejectTiffDisguisedAsJpg()
    {
        var file = CreateFormFile(CreateImageBytes(20, 20, ".tif"), "photo.jpg", "image/jpeg");

        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts");

        urls.ShouldBeNull();
        error.ShouldNotBeNull();
        error.ShouldContain("not a valid image");
    }

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldAcceptUltraHdrJpeg_FromModernPhones()
    {
        // Ultra HDR is the default JPEG flavour on recent Android phones (JPEG + gain map).
        byte[] input;
        using (var sdr = Image.Black(3000, 1500, bands: 3).Linear([1.0], [90.0]).Cast(Enums.BandFormat.Uchar))
        using (var hdr = sdr.Colourspace(Enums.Interpretation.Scrgb))
        {
            input = (byte[])Operation.Call("uhdrsave_buffer", null, hdr);
        }
        Image.FindLoadBuffer(input).ShouldBe("VipsForeignLoadUhdrBuffer");
        var file = CreateFormFile(input, "PXL_20261011.jpg", "image/jpeg");

        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts");

        error.ShouldBeNull();
        using var large = Image.NewFromFile(DiskPath(urls!["large"]));
        using var medium = Image.NewFromFile(DiskPath(urls["medium"]));
        using var thumb = Image.NewFromFile(DiskPath(urls["thumbnail"]));
        large.Width.ShouldBe(1920);
        medium.Width.ShouldBe(800);
        thumb.Width.ShouldBe(400);
    }

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldBoundByWidthOnly_ForPortraitImages()
    {
        var file = CreateFormFile(CreateImageBytes(1200, 2400, ".jpg"), "tall.jpg", "image/jpeg");

        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts");

        error.ShouldBeNull();
        using var large = Image.NewFromFile(DiskPath(urls!["large"]));
        using var medium = Image.NewFromFile(DiskPath(urls["medium"]));
        using var thumb = Image.NewFromFile(DiskPath(urls["thumbnail"]));
        large.Width.ShouldBe(1200);
        medium.Width.ShouldBe(800);
        thumb.Width.ShouldBe(400);
    }

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldNeverUpscale()
    {
        var file = CreateFormFile(CreateImageBytes(300, 200, ".png"), "small.png", "image/png");

        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts");

        error.ShouldBeNull();
        foreach (var key in new[] { "large", "medium", "thumbnail" })
        {
            using var variant = Image.NewFromFile(DiskPath(urls![key]));
            variant.Width.ShouldBe(300);
            variant.Height.ShouldBe(200);
        }
    }

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldDownscaleWideImages_ToLiteralWidths()
    {
        var file = CreateFormFile(CreateImageBytes(3000, 1500, ".jpg"), "wide.jpg", "image/jpeg");

        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts");

        error.ShouldBeNull();
        using var large = Image.NewFromFile(DiskPath(urls!["large"]));
        using var medium = Image.NewFromFile(DiskPath(urls["medium"]));
        using var thumb = Image.NewFromFile(DiskPath(urls["thumbnail"]));
        large.Width.ShouldBe(1920);
        large.Height.ShouldBe(960);
        medium.Width.ShouldBe(800);
        thumb.Width.ShouldBe(400);
    }

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldWriteWebpFiles()
    {
        var file = CreateFormFile(CreateImageBytes(100, 100, ".png"), "a.png", "image/png");

        var (urls, _) = await _service.ProcessAndSaveImageAsync(file, "posts");

        foreach (var url in urls!.Values)
        {
            var header = (await File.ReadAllBytesAsync(DiskPath(url), TestContext.Current.CancellationToken)).Take(12).ToArray();
            Encoding.ASCII.GetString(header, 0, 4).ShouldBe("RIFF");
            Encoding.ASCII.GetString(header, 8, 4).ShouldBe("WEBP");
        }
    }

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldStripExifAndGps()
    {
        byte[] input;
        using (var source = Image.Black(100, 100, bands: 3).Mutate(m =>
               {
                   m.Set(GValue.GIntType, "orientation", 1);
                   m.Set(GValue.GStrType, "exif-ifd0-Make", "SecretCameraMaker");
               }))
        {
            input = source.WriteToBuffer(".jpg");
        }
        using (var check = Image.NewFromBuffer(input))
        {
            check.GetFields().ShouldContain("exif-data");
        }
        var file = CreateFormFile(input, "geo.jpg", "image/jpeg");

        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts");

        error.ShouldBeNull();
        foreach (var url in urls!.Values)
        {
            using var output = Image.NewFromFile(DiskPath(url));
            var fields = output.GetFields();
            fields.ShouldNotContain("exif-data");
            fields.ShouldNotContain(f => f.StartsWith("exif-"));
        }
    }

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldAutoRotateByExifOrientation()
    {
        byte[] input;
        using (var source = Image.Black(800, 400, bands: 3).Mutate(m => m.Set(GValue.GIntType, "orientation", 6)))
        {
            input = source.WriteToBuffer(".jpg");
        }
        var file = CreateFormFile(input, "rot.jpg", "image/jpeg");

        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts");

        error.ShouldBeNull();
        using var large = Image.NewFromFile(DiskPath(urls!["large"]));
        large.Width.ShouldBe(400);
        large.Height.ShouldBe(800);
    }

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldRejectTruncatedJpeg_AndLeaveNoFiles()
    {
        var full = CreateNoisyJpegBytes(800, 600);
        var truncated = full.Take(full.Length / 2).ToArray();
        var file = CreateFormFile(truncated, "cut.jpg", "image/jpeg");

        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts");

        urls.ShouldBeNull();
        error.ShouldNotBeNull();
        error.ShouldContain("not a valid image");
        var dir = Path.Combine(_tempWebRoot, "uploads", "posts");
        if (Directory.Exists(dir))
            Directory.GetFiles(dir).ShouldBeEmpty();
    }

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldKeepAnimation_ForEditorUploads()
    {
        var file = CreateFormFile(CreateAnimatedGifBytes(40, 30, 3), "anim.gif", "image/gif");

        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts/editor");

        error.ShouldBeNull();
        using var output = Image.NewFromFile(DiskPath(urls!["large"]), kwargs: new VOption { { "n", -1 } });
        PageCount(output).ShouldBe(3);
    }

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldFlattenAnimation_ForFeaturedUploads()
    {
        var file = CreateFormFile(CreateAnimatedGifBytes(40, 30, 3), "anim.gif", "image/gif");

        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts");

        error.ShouldBeNull();
        foreach (var url in urls!.Values)
        {
            using var output = Image.NewFromFile(DiskPath(url), kwargs: new VOption { { "n", -1 } });
            PageCount(output).ShouldBe(1);
            output.Height.ShouldBe(30);
        }
    }

    [Fact]
    public async Task ProcessAndSaveImageAsync_ShouldRejectAnimationExceedingTotalPixelCap()
    {
        // 9 frames of 8000x1000 = 72M pixels, above the 8192x8192 (67M) cap, while each frame is within limits
        var file = CreateFormFile(CreateAnimatedGifBytes(8000, 1000, 9), "huge.gif", "image/gif");

        var (urls, error) = await _service.ProcessAndSaveImageAsync(file, "posts/editor");

        urls.ShouldBeNull();
        error.ShouldNotBeNull();
    }

    // ── deletion ────────────────────────────────────────────────────────────

    private string CreateUpload(string relativePath)
    {
        var full = Path.Combine(_tempWebRoot, "uploads", relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, "x");
        return full;
    }

    [Fact]
    public void DeleteStoredImages_RemovesEverySizeInTheStoredJson()
    {
        // Arrange
        var large = CreateUpload("posts/abc-large.webp");
        var medium = CreateUpload("posts/abc-medium.webp");
        var thumb = CreateUpload("posts/abc-thumbnail.webp");
        var unrelated = CreateUpload("posts/other-large.webp");

        var stored = """{"large":"/uploads/posts/abc-large.webp","medium":"/uploads/posts/abc-medium.webp","thumbnail":"/uploads/posts/abc-thumbnail.webp"}""";

        // Act
        _service.DeleteStoredImages(stored);

        // Assert
        File.Exists(large).ShouldBeFalse();
        File.Exists(medium).ShouldBeFalse();
        File.Exists(thumb).ShouldBeFalse();
        File.Exists(unrelated).ShouldBeTrue();
    }

    [Fact]
    public void DeleteStoredImages_WithLegacyPlainUrl_RemovesTheFile()
    {
        // Older rows stored a bare URL rather than a JSON blob.
        var file = CreateUpload("posts/legacy.webp");

        _service.DeleteStoredImages("/uploads/posts/legacy.webp");

        File.Exists(file).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://cdn.example.com/remote.png")]
    public void DeleteStoredImages_IgnoresValuesThatAreNotLocalUploads(string? stored)
    {
        var untouched = CreateUpload("posts/keep.webp");

        Should.NotThrow(() => _service.DeleteStoredImages(stored));

        File.Exists(untouched).ShouldBeTrue();
    }

    [Fact]
    public void DeleteStoredImages_RefusesToEscapeTheUploadsFolder()
    {
        // Arrange: a value crafted to climb out of wwwroot/uploads.
        var outside = Path.Combine(_tempWebRoot, "appsettings.json");
        File.WriteAllText(outside, "secret");

        // Act
        _service.DeleteStoredImages("/uploads/../appsettings.json");

        // Assert
        File.Exists(outside).ShouldBeTrue();
    }

    [Fact]
    public void DeleteUnreferencedImages_KeepsFilesMentionedInPostContent()
    {
        // Arrange
        var referenced = CreateUpload("posts/editor/used.webp");
        var orphan = CreateUpload("posts/editor/unused.webp");

        var content = new[] { "Intro ![alt](/uploads/posts/editor/used.webp) outro" };

        // Act
        var deleted = _service.DeleteUnreferencedImages("posts/editor", content);

        // Assert
        deleted.ShouldBe(1);
        File.Exists(referenced).ShouldBeTrue();
        File.Exists(orphan).ShouldBeFalse();
    }

    [Fact]
    public void DeleteUnreferencedImages_WhenFolderIsMissing_ReturnsZero()
    {
        _service.DeleteUnreferencedImages("posts/editor", []).ShouldBe(0);
    }
}
