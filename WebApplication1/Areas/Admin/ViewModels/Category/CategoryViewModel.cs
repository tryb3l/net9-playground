
namespace WebApplication1.Areas.Admin.ViewModels.Category;

public class CategoryViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int PostCount { get; set; }
}
