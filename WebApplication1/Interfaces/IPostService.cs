using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication1.Areas.Admin.ViewModels.Posts;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Interfaces;

public interface IPostService
{
    Task<BlogIndexViewModel> GetBlogIndexViewModelAsync(int page, string? category, string? tag);
    Task<Post?> GetPostByIdAsync(int id, bool includeUnpublished = false);
    Task<PostListViewModel> GetPostListAsync(int page, string? searchTerm, string? tagFilter, bool? publishedOnly);
    Task<PostViewModel?> GetPostViewModelAsync(int id);
    Task<EditPostViewModel?> GetPostForEditAsync(int id);
    Task<Post> CreatePostAsync(CreatePostViewModel viewModel, string userId);
    Task UpdatePostAsync(int id, EditPostViewModel viewModel);
    Task DeletePostAsync(int id);
    Task<bool> PostExistsAsync(int id);
    Task<List<SelectListItem>> GetAvailableTagsAsync();
    Task PublishPostAsync(int id);
    Task UnpublishPostAsync(int id);
    Task SoftDeletePostAsync(int id);
    Task RestorePostAsync(int id);
}
