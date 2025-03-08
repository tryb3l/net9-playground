using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Areas.Admin.ViewModels.Categories;

public class CreateCategoryViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

}
