using System.Text.RegularExpressions;
using Markdig;

namespace WebApp.Utils;

public static class MarkdownExcerpt
{
    public static string ToPlainText(string? markdown, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var plainText = Markdown.ToPlainText(markdown);
        plainText = Regex.Replace(plainText, @"\s+", " ").Trim();

        if (plainText.Length <= maxLength)
            return plainText;

        var lastSpace = plainText.LastIndexOf(' ', maxLength);
        if (lastSpace > maxLength / 2)
            return plainText[..lastSpace] + "...";

        return plainText[..maxLength] + "...";
    }
}
