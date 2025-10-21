namespace WebApp.ViewModels;

public class PostCardViewModel
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public Dictionary<string, string> FeaturedImageUrls { get; init; } = [];
    public string ShortDescription { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string CategorySlug { get; init; } = string.Empty;
    public DateTime PublishDate { get; init; }
    public IEnumerable<TagViewModel> Tags { get; init; } = new List<TagViewModel>();
}

public class TagViewModel
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int PostCount { get; init; } = 0;
}

public class CategoryViewModel
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int PostCount { get; init; } = 0;
}