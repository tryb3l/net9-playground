using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Areas.Admin.ViewModels.Category;

public class CreateCategoryViewModel
{
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; init; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; init; }
}
