using WebApplication1.Models;

namespace WebApplication1.ViewModels;

public class BlogIndexViewModel
{
    public List<PostCardViewModel> Posts { get; set; } = [];
    public List<CategoryViewModel> Categories { get; set; } = [];
    public List<TagViewModel> Tags { get; set; } = [];
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string? Category { get; set; }
    public string? Tag { get; set; }
    public string? CurrentCategory { get; set; }
    public string? CurrentTag { get; set; }
}