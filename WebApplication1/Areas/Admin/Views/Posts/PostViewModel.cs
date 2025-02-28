using System;

namespace WebApplication1.Areas.Admin.Views.Posts;

public class PostViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedDate { get; set; }
    public bool IsPublished { get; set; }
    public string? AuthorName { get; set; } = string.Empty;
    public string? AuthorId { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new List<string>();
}
