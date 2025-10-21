using WebApp.Models;

namespace WebApp.ViewModels;

public class BlogIndexViewModel
{
    public List<PostCardViewModel> Posts { get; init; } = [];
    public List<CategoryViewModel> Categories { get; init; } = [];
    public List<TagViewModel> Tags { get; init; } = [];
    public int CurrentPage { get; init; }
    public int TotalPages { get; init; }
    public string? Category { get; init; }
    public string? Tag { get; init; }
    public string? CurrentCategory { get; init; }
    public string? CurrentTag { get; init; }
}