namespace WebApplication1.ViewModels;

public class PostCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? FeaturedImageUrl { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public DateTime PublishDate { get; set; }
    public IEnumerable<TagViewModel> Tags { get; set; } = new List<TagViewModel>();
}

public class TagViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class CategoryViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
