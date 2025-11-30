using Shouldly;
using WebApp.Utils;
using Xunit;

namespace WebApp.UnitTests.Helpers;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  Spaces  Around  ", "spaces-around")]
    [InlineData("C# is Great!", "c-is-great")]
    [InlineData("Multiple---Dashes", "multiple-dashes")]
    [InlineData("Mixed CASE Input", "mixed-case-input")]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void GenerateSlug_ShouldNormalizeString(string? input, string expected)
    {
        // Act
        var result = SlugHelper.GenerateSlug(input);

        // Assert
        result.ShouldBe(expected);
    }
}