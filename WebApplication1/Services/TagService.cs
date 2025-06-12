using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication1.Areas.Admin.ViewModels.Tag;
using WebApplication1.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Services;

public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;

    public TagService(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<Tag?> GetTagByIdAsync(int id)
    {
        return await _tagRepository.GetByIdAsync(id);
    }

    public async Task CreateTagAsync(Tag tag)
    {
        await _tagRepository.AddAsync(tag);
        await _tagRepository.SaveChangesAsync();
    }

    public async Task UpdateTagAsync(Tag tag)
    {
        await _tagRepository.UpdateAsync(tag);
        await _tagRepository.SaveChangesAsync();
    }

    public async Task DeleteTagAsync(int id)
    {
        var tag = await _tagRepository.GetByIdAsync(id);
        if (tag != null)
        {
            await _tagRepository.DeleteAsync(tag);
            await _tagRepository.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Tag>> GetPopularTagsAsync(int count)
    {
        return await _tagRepository.GetPopularTagsAsync(count);
    }

    public async Task<IEnumerable<Tag>> GetAllTagsAsync()
    {
        return await _tagRepository.GetAllAsync();
    }

    public async Task<IEnumerable<TagViewModel>> GetAllTagsWithCountAsync()
    {
        var tags = await _tagRepository.GetAllAsync();
        return tags.Select(t => new TagViewModel
        {
            Id = t.Id,
            Name = t.Name,
            PostCount = t.PostTags?.Count ?? 0
        });
    }

    public async Task<bool> TagExistsAsync(int id)
    {
        var tag = await _tagRepository.GetByIdAsync(id);
        return tag != null;
    }

    public async Task<List<SelectListItem>> GetAvailableTagsAsync()
    {
        var tags = await _tagRepository.GetAllAsync();
        return [.. tags.Select(t => new SelectListItem
        {
            Value = t.Id.ToString(),
            Text = t.Name
        })];
    }
}
