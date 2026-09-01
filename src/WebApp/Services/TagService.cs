using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WebApp.Areas.Admin.ViewModels.Tag;
using WebApp.Data;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.Utils;

namespace WebApp.Services;

public class TagService : ITagService
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    private static class CacheKeys
    {
        public static string AvailableTagsSelectList => "AvailableTagsSelectList";
    }

    public TagService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Tag?> GetTagByIdAsync(int id)
    {
        return await _db.Tags.FindAsync(id);
    }

    public async Task CreateTagAsync(Tag tag)
    {
        tag.Slug = await EnsureUniqueSlugAsync(SlugHelper.GenerateSlug(tag.Name));
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync();
        InvalidateTagCache();
    }

    public async Task UpdateTagAsync(Tag tag)
    {
        tag.Slug = await EnsureUniqueSlugAsync(SlugHelper.GenerateSlug(tag.Name), tag.Id);
        _db.Tags.Update(tag);
        await _db.SaveChangesAsync();
        InvalidateTagCache();
    }

    public async Task DeleteTagAsync(int id)
    {
        var tag = await _db.Tags.FindAsync(id);
        if (tag != null)
        {
            _db.Tags.Remove(tag);
            await _db.SaveChangesAsync();
            InvalidateTagCache();
        }
    }

    public async Task<(Tag Tag, bool Created)> GetOrCreateTagAsync(string name)
    {
        var trimmed = name.Trim();

        var existing = await _db.Tags
            .FirstOrDefaultAsync(t => t.Name.ToLower() == trimmed.ToLower());
        if (existing != null)
        {
            return (existing, false);
        }

        var tag = new Tag { Name = trimmed };
        tag.Slug = await EnsureUniqueSlugAsync(SlugHelper.GenerateSlug(trimmed));
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync();
        InvalidateTagCache();

        return (tag, true);
    }

    /// <summary>
    /// IX_Tags_Slug is unique, so two tags whose names normalise identically would collide.
    /// </summary>
    private async Task<string> EnsureUniqueSlugAsync(string slug, int? tagId = null)
    {
        var baseSlug = string.IsNullOrEmpty(slug) ? "tag" : slug;

        var candidate = baseSlug;
        var counter = 1;

        while (await _db.Tags.AnyAsync(t => t.Slug == candidate && (!tagId.HasValue || t.Id != tagId.Value)))
        {
            var suffix = $"-{counter++}";
            candidate = SlugHelper.Truncate(baseSlug, SlugHelper.MaxSlugLength - suffix.Length) + suffix;
        }

        return candidate;
    }

    public async Task<IEnumerable<TagViewModel>> GetAllTagsWithCountAsync()
    {
        // Not cached: PostCount depends on live Post state, and TagService has no visibility
        // into post writes. Caching this previously left stale counts after every publish or
        // trash action. The query itself is trivial at personal-blog scale.
        return await _db.Tags
            .AsNoTracking()
            .Select(t => new TagViewModel
            {
                Id = t.Id,
                Name = t.Name,
                PostCount = t.PostTags.Count
            })
            .OrderByDescending(t => t.PostCount)
            .ToListAsync();
    }

    public async Task<List<SelectListItem>> GetAvailableTagsAsync()
    {
        return await _cache.GetOrCreateAsync(CacheKeys.AvailableTagsSelectList, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await _db.Tags
                .AsNoTracking()
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = t.Name
                })
                .ToListAsync();
        }) ?? [];
    }

    private void InvalidateTagCache()
    {
        _cache.Remove(CacheKeys.AvailableTagsSelectList);
    }
}
