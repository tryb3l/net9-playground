using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebApplication1.Areas.Admin.ViewModels.Post;

public class EditPostViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(255, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 255 characters")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Content")]
    public string? Content { get; set; }

    [Display(Name = "Published")]
    public bool IsPublished { get; set; }

    [Display(Name = "Publication Date")]
    [DataType(DataType.DateTime)]
    public DateTime? PublishedDate { get; set; }

    public DateTime CreatedAt { get; set; }

    [Display(Name = "Tags")]
    public List<int> SelectedTagIds { get; set; } = new List<int>();

    public List<SelectListItem> AvailableTags { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> AvailableCateogries { get; set; } = new List<SelectListItem>();
}
