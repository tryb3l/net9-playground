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
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostTags)
            .ThenInclude(pt => pt.Tag)
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.PublishedDate)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountPublishedPostsAsync()
    {
        return await _context.Posts
            .Where(p => p.IsPublished)
            .CountAsync();
    }

    public async Task<Post> GetPostWithDetailsAsync(int id)
    {
        var post = await _context.Posts
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
            .Include(p => p.Author)
            .Include(p => p.PostTags)
            .ThenInclude(pt => pt.Tag)
            .AsQueryable();

        if(!sbyte.IsNullOrEmpty(searchForm))
        {
            postQuery = postQuery.Where(p => p.Title)
        }
    }
}
