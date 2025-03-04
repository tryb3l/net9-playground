using System;
using WebApplication1.Models;

namespace WebApplication1.ViewModels;

public class BlogIndexViewModel
{
    public List<Post> Posts { get; set; } = new List<Post>();
    public List<Category> Categories { get; set; } = new List<Category>();
    public List<Tag> Tags { get; set; } = new List<Tag>();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string? Category { get; set; }
    public string? Tag { get; set; }
    public string? CurrentCategory { get; set; }
    public string? CurrentTag { get; set; }
}
