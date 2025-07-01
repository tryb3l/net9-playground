using System;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;

    public CategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Category entity)
    {
        await _context.Categories.AddAsync(entity);
    }

    public Task DeleteAsync(Category entity)
    {
        _context.Categories.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _context.Categories.Include(c => c.Posts).ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        return await _context.Categories.Include(c => c.Posts).FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public Task UpdateAsync(Category entity)
    {
        _context.Categories.Update(entity);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistingAsync(int id)
    {
        return await _context.Categories.AnyAsync(c => c.Id == id);
    }
    
    public async Task<int> CountAllAsync()
    {
        return await _context.Categories.CountAsync();
    }
}
