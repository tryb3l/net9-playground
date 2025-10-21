using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.Areas.Admin.ViewModels.Post;

public class EditPostViewModel
{
    public int Id { get; init; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 255 characters")]
    public string Title { get; init; } = string.Empty;
    public bool PublishNow { get; set; }

    [Display(Name = "Content")]
    public string? Content { get; init; }
    public string? FeaturedImageUrl { get; set; }

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

    public List<SelectListItem> AvailableTags { get; set; } = [];
    public List<SelectListItem> AvailableCategories { get; set; } = [];
}
