namespace WebApplication1.Areas.Admin.ViewModels.Posts;

public class PostListViewModel
{
    public List<PostSummaryViewModel> Posts { get; set; } = new List<PostSummaryViewModel>();
    public int TotalPosts { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? TagFilter { get; set; }
    public bool ShowPublishedOnly { get; set; }
}

public class PostSummaryViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsPublished { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new List<string>();
}