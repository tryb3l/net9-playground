using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.Areas.Admin.ViewModels.Post;

public class EditPostViewModel
{
    public int Id { get; init; }
    public string? Slug { get; init; }
    public bool IsDeleted { get; init; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(128, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 128 characters")]
    public string Title { get; init; } = string.Empty;
    public bool PublishNow { get; set; }

    [Display(Name = "Content")]
    public string? Content { get; init; }
    public string? FeaturedImageUrl { get; set; }
    public string? FeaturedImageAlt { get; set; }

    [Display(Name = "Published")]
    public bool IsPublished { get; init; }

    [Display(Name = "Publication Date")]
    [DataType(DataType.DateTime)]
    public DateTime? PublishedDate { get; init; }

    public DateTime CreatedAt { get; init; }
    [Display(Name = "Category")]
    public int? CategoryId { get; init; }
    [Display(Name = "Tags")]
    public List<int> SelectedTagIds { get; init; } = [];

    [ValidateNever]
    public List<SelectListItem> AvailableTags { get; set; } = [];

    [ValidateNever]
    public List<SelectListItem> AvailableCategories { get; set; } = [];
}
