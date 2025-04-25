using System;

namespace WebApplication1.Models;

public class Category
{
    public int Id { get; init; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? Slug { get; set; }
    public ICollection<Post> Posts { get; init; } = new List<Post>();
}
