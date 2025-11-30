using AngleSharp;
using AngleSharp.Dom;

namespace WebApp.IntegrationTests.Support.Extensions;

public static class HtmlParsingExtensions
{
    public static async Task<IDocument> ParseHtmlAsync(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var context = BrowsingContext.New(Configuration.Default);
        return await context.OpenAsync(req => req.Content(content));
    }
}