namespace WebApp.Utils;

/// <summary>
/// Resolves the summary shown on cards, in the feed and in meta descriptions: the
/// author-written excerpt when there is one, otherwise the opening of the post body.
/// </summary>
public static class PostExcerpt
{
    public const int DerivedLength = 200;

    public static string Resolve(string? excerpt, string? content) =>
        string.IsNullOrWhiteSpace(excerpt)
            ? MarkdownExcerpt.ToPlainText(content, DerivedLength)
            : excerpt.Trim();
}
