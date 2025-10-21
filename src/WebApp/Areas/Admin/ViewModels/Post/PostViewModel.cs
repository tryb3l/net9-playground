
namespace WebApp.Areas.Admin.ViewModels.Post;

public class PostViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Content { get; set; }
    public string? FeaturedImageUrl { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime PublishedDate { get; set; }
    public bool IsPublished { get; init; }
    public bool IsDeleted { get; init; }
    public string? AuthorName { get; init; } = string.Empty;
    public string? AuthorId { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = [];
    public string? Status { get; set; }
    public string? Actions { get; set; }

}
