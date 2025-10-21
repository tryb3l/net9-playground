using System;
using WebApp.Models;

namespace WebApp.Interfaces;

public interface ITagRepository : IRepository<Tag>
{
    Task<IEnumerable<Tag>> GetPopularTagsAsync(int count);
    Task<int> CountAllAsync();
}
