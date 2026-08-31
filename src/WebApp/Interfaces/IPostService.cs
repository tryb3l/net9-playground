using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Areas.Admin.ViewModels.Post;
using WebApp.Helpers;
using WebApp.Models;
using WebApp.ViewModels;

namespace WebApp.Interfaces;

public interface IPostService
{
    Task<BlogIndexViewModel> GetBlogIndexViewModelAsync(int page, string? category, string? tag);
    Task<Post?> GetPostByIdAsync(int id, bool includeUnpublished = false, bool includeDeleted = false);
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

    /// <summary>Permanently deletes every trashed post. Returns how many were removed.</summary>
    Task<int> EmptyTrashAsync();

    /// <summary>Restores every trashed post. Returns how many were restored.</summary>
    Task<int> RestoreAllPostsAsync();

    /// <summary>
    /// Deletes editor-uploaded images that no post body references. Returns how many went.
    /// </summary>
    Task<int> CleanupOrphanedEditorImagesAsync();
}
