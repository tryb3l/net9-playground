using System;
using WebApplication1.Models;

namespace WebApplication1.Interfaces;

public interface ITagService
{
    Task<IEnumerable<Tag>> GetAllTagsAsync();
    Task<Tag?> GetTagByIdAsync(int id);
    Task CreateTagAsync(Tag tag);
    Task UpdateTagAsync(Tag tag);
    Task DeleteTagAsync(int id);
    Task<IEnumerable<Tag>> GetPopularTagsAsync(int count);
}
