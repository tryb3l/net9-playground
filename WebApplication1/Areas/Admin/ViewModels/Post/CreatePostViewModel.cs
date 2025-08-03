using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApplication1.Areas.Admin.ViewModels.Post;

public class CreatePostViewModel
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 255 characters")]
    public string Title { get; init; } = string.Empty;

    [Display(Name = "Content")]
    public string? Content { get; init; }
    public string? FeaturedImageUrl { get; set; }

    [Display(Name = "Category")]
    public int? CategoryId { get; init; }

    [Display(Name = "Publish now")]
    public bool PublishNow { get; init; }

    [Display(Name = "Tags")]
    public List<int> SelectedTagIds { get; init; } = [];
    public List<SelectListItem> AvailableTags { get; set; } = [];
    public List<SelectListItem> AvailableCategories { get; set; } = [];
}
