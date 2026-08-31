using System;
using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

public class Tag
{
    public int Id { get; init; }
    [MaxLength(64)]
    public required string Name { get; set; }
    [MaxLength(64)]
    public string? Slug { get; set; }
    public ICollection<PostTag> PostTags { get; init; } = [];
}
