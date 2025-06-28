using System;
using WebApplication1.Models;

namespace WebApplication1.Interfaces;

public interface IPostRepository : IRepository<Post>
{
    Task<IEnumerable<Post>> GetPublishedPostsAsync(int skip, int take);
    Task<Post> GetPostWithDetailsAsync(int id);
    Task<int> CountPublishedPostsAsync();
    Task<IEnumerable<Post>> GetPostsWithFiltersAsync(string? searchTerm, string? tagFilter, bool? publishedOnly, int skip, int take);
    Task<int> CountPostsWithFiltersAsync(string? searchTerm, string? tagFilter, bool? publishedOnly);
    Task DeletePostTagsAsync(IEnumerable<PostTag> postTags);
    Task<bool> PostExistsAsync(int id);
    Task<IEnumerable<Tag>> GetAllTagsAsync();
    Task AddPostTagAsync(PostTag postTag);
    Task<bool> SlugExistsAsync(string slug, int? excludePostId = null);
    Task<(IEnumerable<Post> Posts, int FilteredCount, int TotalCount)> GetPostsForDataTableAsync(
        int start, 
        int length, 
        string? searchTerm, 
        string sortColumn, 
        bool orderAsc);
}