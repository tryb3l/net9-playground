using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Areas.Admin.ViewModels.Category;

public class EditCategoryViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}