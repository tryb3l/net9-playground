using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WebApp.Utils;

public static partial class SlugHelper
{
    public static string GenerateSlug(string? text)
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
        slug = MyRegex().Replace(slug, string.Empty);

        // Replace spaces with hyphens
        slug = MyRegex1().Replace(slug, "-");

        // Remove multiple consecutive hyphens
        slug = MyRegex2().Replace(slug, "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        return slug;
    }

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex MyRegex();
    [GeneratedRegex(@"\s+")]
    private static partial Regex MyRegex1();
    [GeneratedRegex(@"-+")]
    private static partial Regex MyRegex2();
}
