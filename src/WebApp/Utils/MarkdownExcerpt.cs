using System.Text.RegularExpressions;
using Markdig;

namespace WebApp.Utils;

public static partial class MarkdownExcerpt
{
    public static string ToPlainText(string? markdown, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var plainText = Markdown.ToPlainText(markdown);
        plainText = WhitespaceRegex().Replace(plainText, " ").Trim();

        if (plainText.Length <= maxLength)
            return plainText;

        var lastSpace = plainText.LastIndexOf(' ', maxLength);
        if (lastSpace > maxLength / 2)
            return plainText[..lastSpace] + "...";

        return plainText[..maxLength] + "...";
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
