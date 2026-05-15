using AutoMapper;
using Shouldly;
using WebApp.Areas.Admin.Mapping;
using Xunit;

namespace WebApp.UnitTests.Mapping;

public class FeaturedImageUrlConverterTests
{
    private readonly FeaturedImageUrlConverter _sut = new();

    [Fact]
    public void Convert_ValidJson_ReturnsDictionary()
    {
        // Arrange
        const string validJson = """{"large":"img_lg.jpg","thumbnail":"img_thumb.jpg"}""";

        // Act
        var result = _sut.Convert(validJson, null!);

        // Assert
        result.ShouldContainKeyAndValue("large", "img_lg.jpg");
        result.ShouldContainKeyAndValue("thumbnail", "img_thumb.jpg");
    }

    [Fact]
    public void Convert_SimpleUrl_ReturnsSingleEntry()
    {
        // Arrange
        const string simpleUrl = "https://example.com/image.jpg";

        // Act
        var result = _sut.Convert(simpleUrl, null!);

        // Assert
        result["large"].ShouldBe(simpleUrl);
        result["original"].ShouldBe(simpleUrl);
    }

    [Fact]
    public void Convert_InvalidJson_ReturnsEmpty()
    {
        // Act
        var result = _sut.Convert("{ broken_json ... }", null!);

        // Assert
        result.ShouldBeEmpty();
    }
}