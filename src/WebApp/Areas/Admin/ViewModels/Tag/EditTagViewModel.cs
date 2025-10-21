using System.ComponentModel.DataAnnotations;

namespace WebApp.Areas.Admin.ViewModels.Tag;

public class EditTagViewModel
{
    public int Id { get; init; }

    [Required(ErrorMessage = "Tag name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Tag name must be between 2 and 50 characters")]
    [Display(Name = "Tag Name")]
    public string Name { get; init; } = string.Empty;
}