using System;
using WebApplication1.Models;

namespace WebApplication1.Interfaces;

public interface ITagRepository : IRepository<Tag>
{
    Task<IEnumerable<Tag>> GetPopularTagsAsync(int count);
}
