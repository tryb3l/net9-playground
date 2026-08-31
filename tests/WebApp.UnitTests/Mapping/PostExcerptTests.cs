using Shouldly;
using WebApp.Utils;
using Xunit;

namespace WebApp.UnitTests.Mapping;

public class PostExcerptTests
{
    [Fact]
    public void Resolve_WhenExcerptIsProvided_UsesItVerbatim()
    {
        var result = PostExcerpt.Resolve("A hand-written summary.", "Body content that would otherwise be derived.");

        result.ShouldBe("A hand-written summary.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_WhenExcerptIsBlank_FallsBackToDerivedContent(string? excerpt)
    {
        var result = PostExcerpt.Resolve(excerpt, "Some **markdown** content.");

        result.ShouldBe("Some markdown content.");
    }

    [Fact]
    public void Resolve_TrimsTheProvidedExcerpt()
    {
        var result = PostExcerpt.Resolve("  padded  ", null);

        result.ShouldBe("padded");
    }
}
