namespace WebApp.ViewModels;

/// <summary>One entry in the public RSS feed.</summary>
public class FeedItemViewModel
{
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public DateTimeOffset PublishedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public IReadOnlyList<string> Categories { get; init; } = [];
}
