using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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

    private static async Task<byte[]> CreateValidJpegBytesAsync(int width = 10, int height = 10)
    {
        using var image = new Image<Rgba32>(width, height);
        using var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms);
        return ms.ToArray();
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
}
