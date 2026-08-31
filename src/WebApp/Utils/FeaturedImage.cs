using System.Text.Json;

namespace WebApp.Utils;

public static class FeaturedImage
{
    public static Dictionary<string, string> ParseUrls(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return new Dictionary<string, string>();
        }

        if (!source.TrimStart().StartsWith('{'))
        {
            return new Dictionary<string, string>
            {
                ["large"] = source,
                ["thumbnail"] = source,
                ["original"] = source
            };
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(source)
                   ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }

    public static string? GetUrl(string? source, string sizeKey)
    {
        if (string.IsNullOrEmpty(source)) return null;
        if (!source.TrimStart().StartsWith('{')) return source;

        try
        {
            var urls = JsonSerializer.Deserialize<Dictionary<string, string>>(source);
            return urls?.GetValueOrDefault(sizeKey) ?? urls?.Values.FirstOrDefault();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string? GetLargeUrl(string? source) => GetUrl(source, "large");
    public static string? GetThumbnailUrl(string? source) => GetUrl(source, "thumbnail");
    
    public static string? Resolve(string? posted, string? stored)
    {
        if (string.IsNullOrWhiteSpace(posted))
        {
            return null;
        }

        return posted == GetLargeUrl(stored) ? stored : posted;
    }
}
