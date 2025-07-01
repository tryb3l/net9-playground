using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Repositories;

public class PostRepository : IPostRepository
{
    private readonly ApplicationDbContext _context;

    public PostRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Post?> GetByIdAsync(int id)
    {
        return await _context.Posts.FindAsync(id);
    }

    public async Task<IEnumerable<Post>> GetAllAsync()
    {
        return await _context.Posts.ToListAsync();
    }

    public async Task AddAsync(Post entity)
    {
        await _context.Posts.AddAsync(entity);
    }

    public Task UpdateAsync(Post entity)
    {
        _context.Posts.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Post entity)
    {
        _context.Posts.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Post>> GetPublishedPostsAsync(int skip, int take)
    {
        return await _context.Posts
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountPublishedPostsAsync()
    {
        return await _context.Posts
            .CountAsync(p => p.IsPublished);
    }

    public async Task<Post> GetPostWithDetailsAsync(int id)
    {
        var post = await _context.Posts
        .IgnoreQueryFilters()
        .Include(p => p.Author)
        .Include(p => p.Category)
        .Include(p => p.PostTags)
        .ThenInclude(pt => pt.Tag)
        .FirstOrDefaultAsync(p => p.Id == id);

        return post ?? throw new KeyNotFoundException($"Post with id {id} not found");
    }

    public async Task<IEnumerable<Post>> GetPostsWithFiltersAsync(string? searchTerm, string? tagFilter, bool? publishedOnly, int skip, int take)
    {
        var postsQuery = _context.Posts
            .IgnoreQueryFilters()
            .Include(p => p.Author)
            .Include(p => p.PostTags)
            .ThenInclude(pt => pt.Tag)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            postsQuery = postsQuery.Where(p => p.Title.Contains(searchTerm) || (p.Content != null && p.Content.Contains(searchTerm)));
        }

        if (!string.IsNullOrEmpty(tagFilter))
        {
            postsQuery = postsQuery.Where(p => p.PostTags.Any(pt => pt.Tag != null && pt.Tag.Name == tagFilter));
        }

        if (publishedOnly.HasValue && publishedOnly.Value)
        {
            postsQuery = postsQuery.Where(p => p.IsPublished);
        }

        return await postsQuery
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountPostsWithFiltersAsync(string? searchTerm, string? tagFilter, bool? publishedOnly)
    {
        var postsQuery = _context.Posts.IgnoreQueryFilters().AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            postsQuery = postsQuery.Where(p => p.Title.Contains(searchTerm) || (p.Content != null && p.Content.Contains(searchTerm)));
        }

        if (!string.IsNullOrEmpty(tagFilter))
        {
            postsQuery = postsQuery.Where(p => p.PostTags.Any(pt => pt.Tag != null && pt.Tag.Name == tagFilter));
        }

        if (publishedOnly.HasValue && publishedOnly.Value)
        {
            postsQuery = postsQuery.Where(p => p.IsPublished);
        }
        return await postsQuery.CountAsync();
    }

    public Task DeletePostTagsAsync(IEnumerable<PostTag> postTags)
    {
        _context.PostTags.RemoveRange(postTags);
        return Task.CompletedTask;
    }

    public async Task<bool> PostExistsAsync(int id)
    {
        return await _context.Posts.AnyAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Tag>> GetAllTagsAsync()
    {
        return await _context.Tags.ToListAsync();
    }

    public async Task AddPostTagAsync(PostTag postTag)
    {
        await _context.PostTags.AddAsync(postTag);
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludePostId = null)
    {
        var query = _context.Posts.AsQueryable();
        if (excludePostId.HasValue)
        {
            query = query.Where(p => p.Id != excludePostId.Value);
        }
        return await query.AnyAsync(p => p.Slug == slug);
    }

    public async Task<(IEnumerable<Post> Posts, int FilteredCount, int TotalCount)> GetPostsForDataTableAsync(
    int start, int length, string? searchTerm, string sortColumn, bool orderAsc, string? statusFilter)
    {
        var baseQuery = _context.Posts.IgnoreQueryFilters().Include(p => p.Author);

        IQueryable<Post> query;

        switch (statusFilter)
        {
            case "Trashed":
                query = baseQuery.Where(p => p.IsDeleted);
                break;
            case "All":
                query = baseQuery;
                break;
            case "Active":
            default:
                query = baseQuery.Where(p => !p.IsDeleted);
                break;
        }

        var totalCount = await query.CountAsync();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(p =>
                p.Title.Contains(searchTerm) ||
                (p.Author != null && p.Author.UserName.Contains(searchTerm))
            );
        }

        var filteredCount = await query.CountAsync();

        Expression<Func<Post, object>> keySelector = sortColumn.ToLower() switch
        {
            "title" => p => p.Title,
            "author" => p => p.Author.UserName,
            "published" => p => p.PublishedDate,
            "created" => p => p.CreatedAt,
            _ => p => p.CreatedAt
        };

        query = orderAsc
            ? query.OrderBy(keySelector)
            : query.OrderByDescending(keySelector);

        var posts = await query
            .Skip(start)
            .Take(length)
            .ToListAsync();

        return (posts, filteredCount, totalCount);
    }

    public async Task<int> CountAllAsync(Expression<Func<Post, bool>>? filter = null)
    {
        var query = _context.Posts.AsQueryable();
        if (filter != null)
        {
            query = query.Where(filter);
        }
        return await query.CountAsync();
    }

    public async Task<IEnumerable<Post>> GetRecentPostsAsync(int count)
    {
        return await _context.Posts
            .Include(p => p.Author)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Post?> GetByIdAsync(int id, bool includeUnpublished, bool includeDeleted)
    {
        var query = _context.Posts.AsQueryable();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!includeUnpublished)
        {
            query = query.Where(p => p.IsPublished);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Post>> GetAllTrashedPostsAsync()
    {
        return await _context.Posts
            .IgnoreQueryFilters()
            .Where(p => p.IsDeleted)
            .Include(p => p.PostTags)
            .ToListAsync();
    }
}