using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApp.Areas.Admin.ViewModels.Post;

public class EditPostViewModel
{
    public int Id { get; init; }

    /// <summary>
    /// Editable while the post is still a draft. Once published, PostController/PostService
    /// ignore this value - changing the slug on a live post would break external links.
    /// </summary>
    [Display(Name = "URL slug")]
    [StringLength(64, ErrorMessage = "Slug must be 64 characters or fewer")]
    [RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "Slug may only contain lowercase letters, numbers and hyphens")]
    public string? Slug { get; set; }

    public bool IsDeleted { get; init; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(128, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 128 characters")]
    public string Title { get; init; } = string.Empty;
    public bool PublishNow { get; set; }

    [Display(Name = "Content")]
    public string? Content { get; init; }
    [Display(Name = "Excerpt")]
    [StringLength(512, ErrorMessage = "Excerpt must be 512 characters or fewer")]
    public string? Excerpt { get; init; }

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
