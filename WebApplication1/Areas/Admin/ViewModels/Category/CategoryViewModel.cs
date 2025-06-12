
namespace WebApplication1.Areas.Admin.ViewModels.Category;

public class CategoryViewModel
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; set; }
    public int PostCount { get; set; }
}
