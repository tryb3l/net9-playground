namespace WebApp.Areas.Admin.ViewModels.Post;

public class PostViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Excerpt { get; set; }
    public string? Content { get; set; }
    public string? CategoryName { get; set; }
    public List<string> Tags { get; set; } = [];
    public string? AuthorName { get; set; }
    public DateTime? PublishedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsPublished { get; set; }
    public bool IsDeleted { get; set; }

    public string? FeaturedImageUrl { get; set; }
    public string? FeaturedImageAlt { get; set; }
}