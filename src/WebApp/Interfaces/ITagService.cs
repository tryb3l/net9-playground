using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Areas.Admin.ViewModels.Tag;
using WebApp.Models;

namespace WebApp.Interfaces;

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
