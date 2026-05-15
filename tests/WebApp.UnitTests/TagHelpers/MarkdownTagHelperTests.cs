using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Shouldly;
using WebApp.TagHelpers;
using Xunit;

namespace WebApp.UnitTests.TagHelpers;

public class MarkdownTagHelperTests
{
    private static async Task<string> RenderAsync(string markdownContent)
    {
        var helper = new MarkdownTagHelper();

        var childContent = new DefaultTagHelperContent();
        childContent.AppendHtml(markdownContent);

        var output = new TagHelperOutput(
            tagName: "markdown",
            attributes: new TagHelperAttributeList(),
            getChildContentAsync: (_, _) => Task.FromResult<TagHelperContent>(childContent));

        var context = new TagHelperContext(
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: "test");

        await helper.ProcessAsync(context, output);

        return output.Content.GetContent();
    }

    [Fact]
    public async Task ProcessAsync_ScriptTag_IsStripped()
    {
        // Arrange
        const string input = "Hello\n\n<script>alert('xss')</script>";

        // Act
        var result = await RenderAsync(input);

        // Assert
        result.ShouldNotContain("<script>");
        result.ShouldNotContain("alert('xss')");
        result.ShouldContain("Hello");
    }

    [Fact]
    public async Task ProcessAsync_JavascriptHref_IsStripped()
    {
        // Arrange
        const string input = "[click me](javascript:alert('xss'))";

        // Act
        var result = await RenderAsync(input);

        // Assert
        result.ShouldNotContain("javascript:");
    }

    [Fact]
    public async Task ProcessAsync_EventHandlerAttribute_IsStripped()
    {
        // Arrange
        const string input = "<p onclick=\"alert('xss')\">text</p>";

        // Act
        var result = await RenderAsync(input);

        // Assert
        result.ShouldNotContain("onclick");
        result.ShouldContain("text");
    }

    [Fact]
    public async Task ProcessAsync_ValidMarkdown_IsRendered()
    {
        // Arrange
        const string input = "## Hello World\n\nSome **bold** text.";

        // Act
        var result = await RenderAsync(input);

        // Assert
        result.ShouldContain("<h2 ");
        result.ShouldContain("Hello World");
        result.ShouldContain("<strong>");
        result.ShouldContain("bold");
    }
}
