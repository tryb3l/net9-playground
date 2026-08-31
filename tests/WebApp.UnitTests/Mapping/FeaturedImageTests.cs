using Shouldly;
using WebApp.Utils;
using Xunit;

namespace WebApp.UnitTests.Mapping;

public class FeaturedImageTests
{
    [Fact]
    public void ParseUrls_ValidJson_ReturnsDictionary()
    {
        // Arrange
        const string validJson = """{"large":"img_lg.jpg","thumbnail":"img_thumb.jpg"}""";

        // Act
        var result = FeaturedImage.ParseUrls(validJson);

        // Assert
        result.ShouldContainKeyAndValue("large", "img_lg.jpg");
        result.ShouldContainKeyAndValue("thumbnail", "img_thumb.jpg");
    }

    [Fact]
    public void ParseUrls_SimpleUrl_ReturnsSingleEntry()
    {
        // Arrange
        const string simpleUrl = "https://example.com/image.jpg";

        // Act
        var result = FeaturedImage.ParseUrls(simpleUrl);

        // Assert
        result["large"].ShouldBe(simpleUrl);
        result["original"].ShouldBe(simpleUrl);
    }

    [Fact]
    public void ParseUrls_InvalidJson_ReturnsEmpty()
    {
        // Act
        var result = FeaturedImage.ParseUrls("{ broken_json ... }");

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetUrl_NullOrEmpty_ReturnsNull()
    {
        // Act
        var result = FeaturedImage.GetUrl(null, "large");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetUrl_SimpleString_ReturnsSameString()
    {
        // Arrange
        const string simpleUrl = "https://example.com/img.jpg";

        // Act
        var result = FeaturedImage.GetUrl(simpleUrl, "large");

        // Assert
        result.ShouldBe(simpleUrl);
    }

    [Fact]
    public void GetUrl_JsonWithTargetKey_ReturnsTargetValue()
    {
        // Arrange
        const string json = """{"large": "img_lg.jpg", "small": "img_sm.jpg"}""";

        // Act
        var result = FeaturedImage.GetUrl(json, "large");

        // Assert
        result.ShouldBe("img_lg.jpg");
    }

    [Fact]
    public void GetUrl_JsonMissingTargetKey_ReturnsFirstValue()
    {
        // Arrange
        const string json = """{"small": "img_sm.jpg", "thumb": "img_th.jpg"}""";

        // Act
        var result = FeaturedImage.GetUrl(json, "large");

        // Assert
        result.ShouldBe("img_sm.jpg");
    }

    [Fact]
    public void GetUrl_InvalidJson_ReturnsNull()
    {
        // Arrange
        const string brokenJson = "{ not_valid_json }";

        // Act
        var result = FeaturedImage.GetUrl(brokenJson, "large");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Resolve_WhenPostedValueIsTheStoredLargeUrl_KeepsTheStoredJson()
    {
        // The edit form's hidden field carries the large URL until a new upload replaces it.
        const string stored = """{"large":"/uploads/large.webp","thumbnail":"/uploads/thumb.webp"}""";

        var result = FeaturedImage.Resolve("/uploads/large.webp", stored);

        result.ShouldBe(stored);
    }

    [Fact]
    public void Resolve_WhenPostedValueIsFreshJson_ReplacesTheStoredValue()
    {
        const string stored = """{"large":"/uploads/old.webp"}""";
        const string uploaded = """{"large":"/uploads/new.webp","thumbnail":"/uploads/new-t.webp"}""";

        var result = FeaturedImage.Resolve(uploaded, stored);

        result.ShouldBe(uploaded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Resolve_WhenCleared_NormalizesToNull(string? posted)
    {
        // The remove button empties the hidden field; "" must not be persisted.
        var result = FeaturedImage.Resolve(posted, """{"large":"/uploads/old.webp"}""");

        result.ShouldBeNull();
    }

    [Fact]
    public void Resolve_WhenThereWasNoImageAndNoneWasPosted_ReturnsNull()
    {
        FeaturedImage.Resolve(null, null).ShouldBeNull();
        FeaturedImage.Resolve("", null).ShouldBeNull();
    }
}
