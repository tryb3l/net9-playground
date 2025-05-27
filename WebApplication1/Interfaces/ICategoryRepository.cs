using System;
using WebApplication1.Models;

namespace WebApplication1.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<bool> ExistingAsync(int id);
}
