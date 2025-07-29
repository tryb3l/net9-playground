using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public class Tag
{
    public int Id { get; init; }
    [MaxLength(64)]
    public required string Name { get; set; }
    public ICollection<PostTag> PostTags { get; init; } = [];
}
