using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApp.Areas.Admin.ViewModels.Tag;
using WebApp.Models;

namespace WebApp.Interfaces;

public interface ITagService
{
    Task<IEnumerable<TagViewModel>> GetAllTagsWithCountAsync();
    Task<Tag?> GetTagByIdAsync(int id);
    Task<List<SelectListItem>> GetAvailableTagsAsync();
    Task CreateTagAsync(Tag tag);
    Task UpdateTagAsync(Tag tag);
    Task DeleteTagAsync(int id);

    /// <summary>
    /// Returns the tag with this name, creating it if absent. Matching is case-insensitive so
    /// the editor cannot spawn "DotNet" alongside "dotnet".
    /// </summary>
    Task<(Tag Tag, bool Created)> GetOrCreateTagAsync(string name);
}
