namespace WebApplication1.Areas.Admin.ViewModels.Post;

public class PostListViewModel
{
    public List<PostSummaryViewModel> Posts { get; init; } = [];
    public int TotalPosts { get; init; }
    public int CurrentPage { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public string? TagFilter { get; init; }
    public bool ShowPublishedOnly { get; init; }
}

public class PostSummaryViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public bool IsPublished { get; set; }
    public bool IsDeleted { get; set; }
    public string AuthorName { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = [];
}