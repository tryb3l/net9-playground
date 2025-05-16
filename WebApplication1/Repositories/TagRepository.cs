using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Repositories;

public class TagRepository : ITagRepository
{
    private readonly ApplicationDbContext _context;

    public TagRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Tag?> GetByIdAsync(int id)
    {
        return await _context.Tags.FindAsync(id);
    }

    public async Task<IEnumerable<Tag>> GetAllAsync()
    {
        return await _context.Tags.Include(t => t.PostTags).ToListAsync();
    }

    public async Task AddAsync(Tag entity)
    {
        await _context.Tags.AddAsync(entity);
    }

    public Task DeleteAsync(Tag entity)
    {
        _context.Tags.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public Task UpdateAsync(Tag entity)
    {
        _context.Tags.Update(entity);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Tag>> GetPopularTagsAsync(int count)
    {
        return await _context.Tags
            .Include(t => t.PostTags)
            .OrderByDescending(t => t.PostTags.Count)
            .Take(count)
            .ToListAsync();
    }
}
