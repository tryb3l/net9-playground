using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

public class Category
{
    public int Id { get; init; }
    [MaxLength(64)]
    public required string Name { get; set; }
    [MaxLength(256)]
    public string? Description { get; set; }
    [MaxLength(64)]
    public string? Slug { get; set; }
    public ICollection<Post> Posts { get; init; } = new List<Post>();
}
