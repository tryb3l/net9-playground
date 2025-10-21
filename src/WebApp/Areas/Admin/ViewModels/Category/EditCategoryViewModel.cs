using System.ComponentModel.DataAnnotations;

namespace WebApp.Areas.Admin.ViewModels.Category;

public class EditCategoryViewModel
{
    public int Id { get; init; }

    [Required]
    [StringLength(100)]
    public required string Name { get; init; }

    [StringLength(500)]
    public string? Description { get; set; }
}