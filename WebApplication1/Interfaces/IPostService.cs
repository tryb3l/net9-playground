using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication1.Areas.Admin.ViewModels.Post;
using WebApplication1.Helpers;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Interfaces;

public interface IPostService
{
    Task<BlogIndexViewModel> GetBlogIndexViewModelAsync(int page, string? category, string? tag);
    Task<Post?> GetPostByIdAsync(int id, bool includeUnpublished = false, bool includeDeleted = false);
    Task<Post?> GetPostBySlugAsync(string slug);
    Task<PostListViewModel> GetPostListAsync(int page, string? searchTerm, string? tagFilter, bool? publishedOnly);
    Task<PostViewModel?> GetPostViewModelAsync(int id);
    Task<PostViewModel?> GetPostViewModelBySlugAsync(string slug);
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
    Task<DataTablesResponse<PostViewModel>> GetPostListForDataTableAsync(DataTablesRequest request);
    Task EmptyTrashAsync();
    Task RestoreAllPostsAsync();
}
