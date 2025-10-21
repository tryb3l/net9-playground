using System;
using WebApp.Models;

namespace WebApp.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<bool> ExistingAsync(int id);
    Task<int> CountAllAsync();
}
