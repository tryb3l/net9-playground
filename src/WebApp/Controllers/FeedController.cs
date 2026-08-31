using System.Globalization;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using WebApp.Interfaces;

namespace WebApp.Controllers;

public class FeedController : Controller
{
    private const int FeedItemCount = 20;

    private readonly IPostService _postService;

    public FeedController(IPostService postService)
    {
        _postService = postService;
    }

    [HttpGet("/feed.rss")]
    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Rss()
    {
        var items = await _postService.GetFeedItemsAsync(FeedItemCount);
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var feedUrl = $"{baseUrl}/feed.rss";

        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            Async = true
        };

        using var stream = new MemoryStream();
        await using (var writer = XmlWriter.Create(stream, settings))
        {
            await writer.WriteStartDocumentAsync();
            await writer.WriteStartElementAsync(null, "rss", null);
            await writer.WriteAttributeStringAsync(null, "version", null, "2.0");
            await writer.WriteAttributeStringAsync("xmlns", "atom", null, "http://www.w3.org/2005/Atom");

            await writer.WriteStartElementAsync(null, "channel", null);
            await writer.WriteElementStringAsync(null, "title", null, "Blog");
            await writer.WriteElementStringAsync(null, "link", null, $"{baseUrl}/blog");
            await writer.WriteElementStringAsync(null, "description", null, "Latest posts");
            await writer.WriteElementStringAsync(null, "language", null, "en");

            // Self reference, so readers can rediscover the feed if it is copied around.
            await writer.WriteStartElementAsync("atom", "link", "http://www.w3.org/2005/Atom");
            await writer.WriteAttributeStringAsync(null, "href", null, feedUrl);
            await writer.WriteAttributeStringAsync(null, "rel", null, "self");
            await writer.WriteAttributeStringAsync(null, "type", null, "application/rss+xml");
            await writer.WriteEndElementAsync();

            if (items.Count > 0)
            {
                await writer.WriteElementStringAsync(null, "lastBuildDate", null, ToRfc1123(items[0].UpdatedAt ?? items[0].PublishedAt));
            }

            foreach (var item in items)
            {
                var url = $"{baseUrl}/blog/{item.Slug}";

                await writer.WriteStartElementAsync(null, "item", null);
                await writer.WriteElementStringAsync(null, "title", null, item.Title);
                await writer.WriteElementStringAsync(null, "link", null, url);
                await writer.WriteElementStringAsync(null, "description", null, item.Summary);
                await writer.WriteElementStringAsync(null, "pubDate", null, ToRfc1123(item.PublishedAt));

                await writer.WriteStartElementAsync(null, "guid", null);
                await writer.WriteAttributeStringAsync(null, "isPermaLink", null, "true");
                await writer.WriteStringAsync(url);
                await writer.WriteEndElementAsync();

                foreach (var category in item.Categories)
                {
                    await writer.WriteElementStringAsync(null, "category", null, category);
                }

                await writer.WriteEndElementAsync();
            }

            await writer.WriteEndElementAsync(); // channel
            await writer.WriteEndElementAsync(); // rss
            await writer.WriteEndDocumentAsync();
            await writer.FlushAsync();
        }

        return File(stream.ToArray(), "application/rss+xml; charset=utf-8");
    }

    private static string ToRfc1123(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("r", CultureInfo.InvariantCulture);
}
