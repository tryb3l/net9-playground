using AutoMapper;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using WebApp.Areas.Admin.ViewModels.Tag;
using WebApp.Interfaces;
using WebApp.Models;

namespace WebApp.Services;

public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    private static class CacheKeys
    {
        public static string AllTags => "AllTags";
        public static string AllTagsWithCount => "AllTagsWithCount";
        public static string AvailableTagsSelectList => "AvailableTagsSelectList";
        public static string PopularTags(int count) => $"PopularTags-{count}";
        public static string TagById(int id) => $"Tag-{id}";
    }

    public TagService(ITagRepository tagRepository, IMapper mapper, IMemoryCache cache)
    {
        _tagRepository = tagRepository;
        _cache = cache;
        _mapper = mapper;
    }

    public async Task<Tag?> GetTagByIdAsync(int id)
    {
        return await _cache.GetOrCreateAsync(CacheKeys.TagById(id), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await _tagRepository.GetByIdAsync(id);
        });
    }

    public async Task CreateTagAsync(Tag tag)
    {
        await _tagRepository.AddAsync(tag);
        await _tagRepository.SaveChangesAsync();
        InvalidateTagCache();
    }

    public async Task UpdateTagAsync(Tag tag)
    {
        await _tagRepository.UpdateAsync(tag);
        await _tagRepository.SaveChangesAsync();
        InvalidateTagCache(tag.Id);
    }

    public async Task DeleteTagAsync(int id)
    {
        var tag = await _tagRepository.GetByIdAsync(id);
        if (tag != null)
        {
            await _tagRepository.DeleteAsync(tag);
            await _tagRepository.SaveChangesAsync();
            InvalidateTagCache(id);
        }
    }

    public async Task<IEnumerable<Tag>> GetPopularTagsAsync(int count)
    {
        return await _cache.GetOrCreateAsync(CacheKeys.PopularTags(count), async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await _tagRepository.GetPopularTagsAsync(count);
        }) ?? [];
    }

    public async Task<IEnumerable<Tag>> GetAllTagsAsync()
    {
        return await _cache.GetOrCreateAsync(CacheKeys.AllTags, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await _tagRepository.GetAllAsync();
        }) ?? [];
    }

    public async Task<IEnumerable<TagViewModel>> GetAllTagsWithCountAsync()
    {
        return await _cache.GetOrCreateAsync(CacheKeys.AllTagsWithCount, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            var tags = await _tagRepository.GetAllAsync();
            var tagViewModels = _mapper.Map<IEnumerable<TagViewModel>>(tags);
            return tagViewModels.OrderByDescending(t => t.PostCount);
        }) ?? Enumerable.Empty<TagViewModel>();
    }

    public async Task<bool> TagExistsAsync(int id)
    {
        var tag = await _tagRepository.GetByIdAsync(id);
        return tag != null;
    }

    public async Task<List<SelectListItem>> GetAvailableTagsAsync()
    {
        return await _cache.GetOrCreateAsync(CacheKeys.AvailableTagsSelectList, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            var tags = await GetAllTagsAsync();
            return _mapper.Map<List<SelectListItem>>(tags);
        }) ?? [];
    }
    
    private void InvalidateTagCache(int? id = null)
    {
        if (id.HasValue)
        {
            _cache.Remove(CacheKeys.TagById(id.Value));
        }
        
        _cache.Remove(CacheKeys.AllTags);
        _cache.Remove(CacheKeys.AllTagsWithCount);
        _cache.Remove(CacheKeys.AvailableTagsSelectList);
        _cache.Remove(CacheKeys.PopularTags(10));
    }
}
