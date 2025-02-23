using System;

namespace WebApplication1.Models;

public class Tag
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<PostTag> PostTags { get; set; } = [];
}
