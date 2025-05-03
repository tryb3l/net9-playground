using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Areas.Admin.ViewModels.Tag;

public class CreateTagViewModel
{
    [Required(ErrorMessage = "Tag name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Tag name must be between 2 and 50 characters")]
    [Display(Name = "Tag Name")]
    public string Name { get; set; } = string.Empty;
}