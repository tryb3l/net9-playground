using System;
using WebApplication1.Models;

namespace WebApplication1.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task AddAsync(Category entity);
    Task DeleteAsync(Category entity);
    Task<IEnumerable<Category>> GetAllAsync();
    Task SaveChangesAsync();
    Task UpdateAsync(Category entity);
    Task<bool> ExistingAsync(int id);
}
