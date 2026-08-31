namespace WebApp.Areas.Admin.ViewModels.Post;

public class PostTagLinkViewModel
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
}

public class PostViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Excerpt { get; set; }
    public string? Content { get; set; }
    public string? CategoryName { get; set; }
    public string? CategorySlug { get; set; }
    public List<PostTagLinkViewModel> Tags { get; set; } = [];
    public string? AuthorName { get; set; }
    public DateTime? PublishedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsPublished { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public string? FeaturedImageUrl { get; set; }
    public string? FeaturedImageAlt { get; set; }
}