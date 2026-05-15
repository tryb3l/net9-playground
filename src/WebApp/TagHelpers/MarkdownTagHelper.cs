using System.Net;
using Ganss.Xss;
using Markdig;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace WebApp.TagHelpers;

[HtmlTargetElement("markdown")]
public class MarkdownTagHelper : TagHelper
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    private static readonly HtmlSanitizer Sanitizer = new();

    static MarkdownTagHelper()
    {
        // Start from a known-clean state rather than extending the library defaults.
        Sanitizer.AllowedTags.Clear();
        Sanitizer.AllowedAttributes.Clear();
        Sanitizer.AllowedSchemes.Clear();
        Sanitizer.AllowedCssProperties.Clear();

        foreach (var tag in new[]
        {
            "h1", "h2", "h3", "h4", "h5", "h6",
            "p", "div", "span", "br", "hr",
            "ul", "ol", "li",
            "blockquote", "pre", "code",
            "strong", "em",
            "a", "img",
            "table", "thead", "tbody", "tr", "th", "td",
        })
        {
            Sanitizer.AllowedTags.Add(tag);
        }

        foreach (var attr in new[]
        {
            "class", "id",
            "href", "src", "alt", "title", "target",
            "width", "height",
        })
        {
            Sanitizer.AllowedAttributes.Add(attr);
        }

        // Allow only safe URI schemes; javascript: / vbscript: / data: are excluded.
        Sanitizer.AllowedSchemes.Add("https");
        Sanitizer.AllowedSchemes.Add("http");
        Sanitizer.AllowedSchemes.Add("mailto");
    }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var childContent = await output.GetChildContentAsync();
        var markdownContent = childContent.GetContent();

        if (string.IsNullOrWhiteSpace(markdownContent))
        {
            output.SuppressOutput();
            return;
        }

        var decodedMarkdown = WebUtility.HtmlDecode(markdownContent);

        var htmlContent = Markdown.ToHtml(decodedMarkdown, Pipeline);

        var sanitizedHtml = Sanitizer.Sanitize(htmlContent);

        output.TagName = null;
        output.Content.SetHtmlContent(sanitizedHtml);
    }
}