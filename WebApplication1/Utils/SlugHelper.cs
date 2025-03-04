using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WebApplication1.Utils;

public static class SlugHelper
{
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Convert to lowercase, replace spaces with hyphens
        var slug = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

        // Remove invalid characters
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", string.Empty);

        // Replace spaces with hyphens
        slug = Regex.Replace(slug, @"\s+", "-");

        // Remove multiple consecutive hyphens
        slug = Regex.Replace(slug, @"-+", "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        return slug;
    }
}
