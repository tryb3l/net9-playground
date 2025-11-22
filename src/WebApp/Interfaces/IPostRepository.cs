using System;
using System.Linq.Expressions;
using WebApp.Models;

namespace WebApp.Interfaces;

public interface IPostRepository : IRepository<Post>
{
    Task<IEnumerable<Post>> GetPublishedPostsAsync(int skip, int take);
    Task<IEnumerable<Post>> GetPublishedPostsWithDetailsAsync(int skip, int take, string? category = null, string? tag = null);
    Task<int> CountPublishedPostsAsync(string? category = null, string? tag = null);
    Task<Post> GetPostWithDetailsAsync(int id);
    Task<IEnumerable<Post>> GetPostsWithFiltersAsync(string? searchTerm, string? tagFilter, bool? publishedOnly, int skip, int take);
    Task<int> CountPostsWithFiltersAsync(string? searchTerm, string? tagFilter, bool? publishedOnly);
    Task DeletePostTagsAsync(IEnumerable<PostTag> postTags);
    Task<bool> PostExistsAsync(int id);
    Task<IEnumerable<Tag>> GetAllTagsAsync();
    Task AddPostTagAsync(PostTag postTag);
    Task<bool> SlugExistsAsync(string slug, int? excludePostId = null);
    Task<int> CountAllAsync(Expression<Func<Post, bool>>? filter = null);
    Task<IEnumerable<Post>> GetRecentPostsAsync(int count);
    Task<Post?> GetByIdAsync(int id, bool includeUnpublished, bool includeDeleted);
    Task<Post?> GetBySlugAsync(string slug);
    Task<(IEnumerable<Post> Posts, int FilteredCount, int TotalCount)> GetPostsForDataTableAsync(
        int start,
        int length,
        string? searchTerm,
        string sortColumn,
        bool orderAsc,
        string? statusFilter);
    Task<IEnumerable<Post>> GetAllTrashedPostsAsync();
}