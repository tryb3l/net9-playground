using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WebApp.Utils;

public static partial class SlugHelper
{
    /// <summary>Width of the Slug columns on Post and Category.</summary>
    public const int MaxSlugLength = 64;

    /// <summary>
    /// Default budget for a generated slug, leaving room for a "-N" uniqueness suffix.
    /// </summary>
    public const int MaxBaseSlugLength = 60;

    public static string GenerateSlug(string? text, int maxLength = MaxBaseSlugLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in from c in normalizedString let unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c) where unicodeCategory != UnicodeCategory.NonSpacingMark select c)
        {
            stringBuilder.Append(c);
        }

        // Convert to lowercase, replace spaces with hyphens
        var slug = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

        // Remove invalid characters
        slug = NonAlphanumericRegex().Replace(slug, string.Empty);

        // Replace spaces with hyphens
        slug = WhitespaceRegex().Replace(slug, "-");

        // Remove multiple consecutive hyphens
        slug = MultipleHyphensRegex().Replace(slug, "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        return Truncate(slug, maxLength);
    }

    /// <summary>
    /// Trims a slug to <paramref name="maxLength"/>, preferring to cut on a hyphen so the
    /// last word isn't chopped in half. Titles may be twice as long as the Slug column.
    /// </summary>
    public static string Truncate(string slug, int maxLength)
    {
        if (maxLength <= 0) return string.Empty;
        if (slug.Length <= maxLength) return slug;

        var cut = slug[..maxLength];
        var lastHyphen = cut.LastIndexOf('-');
        if (lastHyphen > maxLength / 2)
        {
            cut = cut[..lastHyphen];
        }

        return cut.TrimEnd('-');
    }

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex NonAlphanumericRegex();
    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
    [GeneratedRegex(@"-+")]
    private static partial Regex MultipleHyphensRegex();
}
