using System;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Interfaces;

public interface IPostService
{
    Task<BlogIndexViewModel> GetBlogIndexViewModelAsync(int page, string? category, string? tag);
    Task<Post?> GetPostByIdAsync(int id);
}
