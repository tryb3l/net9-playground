using Shouldly;
using WebApp.Areas.Admin.Mapping;
using Xunit;

namespace WebApp.UnitTests.Mapping;

public class SingleUrlConverterTests
{
    private static SingleUrlConverter CreateSut(string key = "large") => new(key);

    [Fact]
    public void Convert_NullOrEmpty_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var result = sut.Convert(null, null!);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Convert_SimpleString_ReturnsSameString()
    {
        // Arrange
        var sut = CreateSut();
        const string simpleUrl = "https://example.com/img.jpg";

        // Act
        var result = sut.Convert(simpleUrl, null!);

        // Assert
        result.ShouldBe(simpleUrl);
    }

    [Fact]
    public void Convert_JsonWithTargetKey_ReturnsTargetValue()
    {
        // Arrange
        var sut = CreateSut("large");
        const string json = """{"large": "img_lg.jpg", "small": "img_sm.jpg"}""";

        // Act
        var result = sut.Convert(json, null!);

        // Assert
        result.ShouldBe("img_lg.jpg");
    }

    [Fact]
    public void Convert_JsonMissingTargetKey_ReturnsFirstValue()
    {
        // Arrange
        var sut = CreateSut("large");
        const string json = """{"small": "img_sm.jpg", "thumb": "img_th.jpg"}""";

        // Act
        var result = sut.Convert(json, null!);

        // Assert
        result.ShouldBe("img_sm.jpg");
    }

    [Fact]
    public void Convert_InvalidJson_ReturnsNull()
    {
        // Arrange
        var sut = CreateSut();
        const string brokenJson = "{ not_valid_json }";

        // Act
        var result = sut.Convert(brokenJson, null!);

        // Assert
        result.ShouldBeNull();
    }
}