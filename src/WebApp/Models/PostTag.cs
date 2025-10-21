using System;
using WebApp.Interfaces;

namespace WebApp.Models;

public class PostTag : ISoftDelete
{
    public int PostId { get; init; }
    public Post? Post { get; init; }
    public int TagId { get; init; }
    public Tag? Tag { get; init; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
