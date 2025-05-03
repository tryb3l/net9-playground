using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication1.Areas.Admin.ViewModels.Tag;
using WebApplication1.Models;

namespace WebApplication1.Interfaces;

public interface ITagService
{
    Task<IEnumerable<Tag>> GetAllTagsAsync();
    Task<IEnumerable<TagViewModel>> GetAllTagsWithCountAsync();
    Task<Tag?> GetTagByIdAsync(int id);
    Task<List<SelectListItem>> GetAvailableTagsAsync();
    Task CreateTagAsync(Tag tag);
    Task UpdateTagAsync(Tag tag);
    Task DeleteTagAsync(int id);
    Task<IEnumerable<Tag>> GetPopularTagsAsync(int count);
    Task<bool> TagExistsAsync(int id);
}
