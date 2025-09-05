using System.ComponentModel.DataAnnotations;
using WebApplication1.Interfaces;

namespace WebApplication1.Models;

public class Post : ISoftDelete
{
    public int Id { get; init; }
    [MaxLength(128)]
    public required string Title { get; set; }
    [MaxLength(64)]
    public string? Slug { get; set; }
    public string? Content { get; set; }
    public string? FeaturedImageUrls { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedDate { get; set; }
    public bool IsPublished { get; set; } = false;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? AuthorId { get; set; }
    public User? Author { get; init; }
    public ICollection<PostTag> PostTags { get; init; } = new List<PostTag>();
    public int? CategoryId { get; set; }
    public Category? Category { get; init; }
}
