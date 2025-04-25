using System;

namespace WebApplication1.Models;

public class PostTag
{
    public int PostId { get; init; }
    public Post? Post { get; init; }
    public int TagId { get; init; }
    public Tag? Tag { get; init; }
}
