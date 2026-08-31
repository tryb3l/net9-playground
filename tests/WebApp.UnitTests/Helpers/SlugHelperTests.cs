using System.Linq;
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

    [Fact]
    public void GenerateSlug_WhenTitleExceedsColumnWidth_TruncatesWithinBudget()
    {
        // Title allows 128 chars; the Slug column is varchar(64).
        var title = string.Join(' ', Enumerable.Repeat("wordy", 25));

        var result = SlugHelper.GenerateSlug(title);

        result.Length.ShouldBeLessThanOrEqualTo(SlugHelper.MaxBaseSlugLength);
    }

    [Fact]
    public void GenerateSlug_WhenTruncating_DoesNotSplitAWord()
    {
        var result = SlugHelper.GenerateSlug("alpha bravo charlie delta echo foxtrot golf hotel india juliet");

        result.ShouldNotEndWith("-");
        // Every retained segment must be a complete word from the original title.
        var words = new[] { "alpha", "bravo", "charlie", "delta", "echo", "foxtrot", "golf", "hotel", "india", "juliet" };
        foreach (var segment in result.Split('-'))
        {
            words.ShouldContain(segment);
        }
    }

    [Fact]
    public void Truncate_LeavesRoomForAUniquenessSuffix()
    {
        var slug = new string('a', 70);

        var result = SlugHelper.Truncate(slug, SlugHelper.MaxSlugLength - "-12".Length);

        (result + "-12").Length.ShouldBeLessThanOrEqualTo(SlugHelper.MaxSlugLength);
    }

    [Fact]
    public void Truncate_WhenSlugAlreadyFits_ReturnsItUnchanged()
    {
        SlugHelper.Truncate("hello-world", SlugHelper.MaxSlugLength).ShouldBe("hello-world");
    }
}