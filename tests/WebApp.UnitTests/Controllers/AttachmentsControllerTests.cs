using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using WebApp.Areas.Admin.Controllers;
using WebApp.Interfaces;
using Xunit;

namespace WebApp.UnitTests.Controllers;

public class AttachmentsControllerTests
{
    private static AttachmentsController CreateSut()
    {
        var mockService = new Mock<IAttachmentService>();
        var mockLogger = new Mock<ILogger<AttachmentsController>>();
        return new AttachmentsController(mockService.Object, mockLogger.Object);
    }

    [Fact]
    public async Task Upload_FileTooLarge_ReturnsBadRequest()
    {
        // Arrange
        var sut = CreateSut();
        var mockFile = new Mock<IFormFile>();
        
        const long elevenMb = 11 * 1024 * 1024;
        mockFile.Setup(f => f.Length).Returns(elevenMb);
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

        // Act
        var result = await sut.Upload(mockFile.Object);

        // Assert
        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        (badRequest.Value?.ToString() ?? throw new InvalidOperationException()).ShouldContain("exceeds the 10MB limit");
    }

    [Fact]
    public async Task Upload_InvalidMimeType_ReturnsBadRequest()
    {
        // Arrange
        var sut = CreateSut();
        var mockFile = new Mock<IFormFile>();
        
        mockFile.Setup(f => f.Length).Returns(1024);
        mockFile.Setup(f => f.ContentType).Returns("application/x-msdownload");

        // Act
        var result = await sut.Upload(mockFile.Object);

        // Assert
        result.ShouldBeOfType<BadRequestObjectResult>();
    }
}