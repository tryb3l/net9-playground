using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Admin.ViewModels.Post;
using WebApp.Data;
using WebApp.Data.Queries;
using WebApp.Helpers;
using WebApp.Interfaces;
using WebApp.Mapping;
using WebApp.Models;
using WebApp.Utils;
using WebApp.ViewModels;

namespace WebApp.Services;

public class PostService : IPostService
{
    private readonly ApplicationDbContext _db;
    private readonly ITagService _tagService;
    private readonly IAttachmentService _attachmentService;

    public PostService(ApplicationDbContext db, ITagService tagService, IAttachmentService attachmentService)
    {
        _db = db;
        _tagService = tagService;
        _attachmentService = attachmentService;
    }

    public async Task<BlogIndexViewModel> GetBlogIndexViewModelAsync(int page, string? category, string? tag)
    {
        const int pageSize = 5;

        var postsQuery = _db.Posts
            .AsNoTracking()
            .Published()
            .InCategory(category)
            .WithTag(tag);

        var totalPosts = await postsQuery.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalPosts / (double)pageSize));

        // ?page=0 would otherwise produce a negative OFFSET, which Postgres rejects -
        // a 500 on a public, crawlable URL.
        page = Math.Clamp(page, 1, totalPages);
        var skip = (page - 1) * pageSize;

        var cards = await postsQuery
            .OrderByDescending(p => p.PublishedDate ?? p.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                p.Content,
                p.FeaturedImageUrls,
                CategoryName = p.Category != null ? p.Category.Name : null,
                CategorySlug = p.Category != null ? p.Category.Slug : null,
                p.PublishedDate,
                p.CreatedAt,
                TagNames = p.PostTags.Where(pt => pt.Tag != null).Select(pt => pt.Tag!.Name).ToList()
            })
            .ToListAsync();

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.Posts.Any(p => p.IsPublished && !p.IsDeleted))
            .Select(c => new CategoryViewModel
            {
                Name = c.Name,
                Slug = c.Slug ?? string.Empty,
                // Drafts and trashed posts are not visible to readers, so they must not be
                // counted here or the sidebar promises more posts than the filter returns.
                PostCount = c.Posts.Count(p => p.IsPublished && !p.IsDeleted)
            })
            .ToListAsync();

        var popularTags = await _db.Tags
            .AsNoTracking()
            .Where(t => t.PostTags.Any(pt => pt.Post != null && pt.Post.IsPublished && !pt.Post.IsDeleted))
            .OrderByDescending(t => t.PostTags.Count(pt => pt.Post != null && pt.Post.IsPublished && !pt.Post.IsDeleted))
            .Take(10)
            .Select(t => new TagViewModel
            {
                Name = t.Name,
                PostCount = t.PostTags.Count(pt => pt.Post != null && pt.Post.IsPublished && !pt.Post.IsDeleted)
            })
            .ToListAsync();

        return new BlogIndexViewModel
        {
            Posts = cards.Select(c => new PostCardViewModel
            {
                Id = c.Id,
                Title = c.Title,
                Slug = c.Slug ?? string.Empty,
                FeaturedImageUrls = FeaturedImage.ParseUrls(c.FeaturedImageUrls),
                ShortDescription = MarkdownExcerpt.ToPlainText(c.Content, 200),
                CategoryName = c.CategoryName ?? "Uncategorized",
                CategorySlug = c.CategorySlug ?? string.Empty,
                PublishDate = c.PublishedDate ?? c.CreatedAt,
                Tags = c.TagNames.Select(n => new TagViewModel { Name = n }).ToList()
            }).ToList(),
            Categories = categories,
            Tags = popularTags,
            CurrentPage = page,
            TotalPages = totalPages,
            CurrentCategory = category,
            CurrentTag = tag,
            Category = category,
            Tag = tag
        };
    }

    public async Task<Post?> GetPostByIdAsync(int id, bool includeUnpublished = false, bool includeDeleted = false)
    {
        var query = _db.Posts.AsQueryable();
        if (includeDeleted)
        {
            query = query.IncludingDeleted();
        }
        if (!includeUnpublished)
        {
            query = query.Published();
        }
        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PostViewModel?> GetPostViewModelAsync(int id)
    {
        var post = await _db.Posts
            .AsNoTracking()
            .IncludingDeleted()
            .WithDetails()
            .FirstOrDefaultAsync(p => p.Id == id);

        return post?.ToPostViewModel();
    }

    public async Task<PostViewModel?> GetPostViewModelBySlugAsync(string slug)
    {
        var post = await _db.Posts
            .AsNoTracking()
            .Published()
            .WithDetails()
            .FirstOrDefaultAsync(p => p.Slug == slug);

        return post?.ToPostViewModel();
    }

    public async Task<EditPostViewModel?> GetPostForEditAsync(int id)
    {
        var post = await _db.Posts
            .IncludingDeleted()
            .WithDetails()
            .FirstOrDefaultAsync(p => p.Id == id);
        if (post == null) return null;

        var viewModel = post.ToEditPostViewModel();

        viewModel.AvailableTags = await GetAvailableTagsAsync();
        viewModel.AvailableCategories = await _db.Categories
            .AsNoTracking()
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
            .ToListAsync();

        return viewModel;
    }

    public async Task<Post> CreatePostAsync(CreatePostViewModel viewModel, string userId)
    {
        var slug = await EnsureUniqueSlugAsync(SlugHelper.GenerateSlug(viewModel.Title));

        var post = new Post
        {
            Title = viewModel.Title,
            Content = viewModel.Content,
            Slug = slug,
            AuthorId = userId,
            CategoryId = viewModel.CategoryId,
            IsPublished = viewModel.PublishNow,
            CreatedAt = DateTime.UtcNow,
            PublishedDate = viewModel.PublishNow ? DateTime.UtcNow : null,
            FeaturedImageUrls = viewModel.FeaturedImageUrl,
            FeaturedImageAlt = viewModel.FeaturedImageAlt
        };

        if (viewModel.SelectedTagIds is { Count: > 0 })
        {
            foreach (var tagId in viewModel.SelectedTagIds)
            {
                post.PostTags.Add(new PostTag { TagId = tagId });
            }
        }

        await _db.Posts.AddAsync(post);
        await _db.SaveChangesAsync();

        return post;
    }

    public async Task UpdatePostAsync(int id, EditPostViewModel viewModel)
    {
        var post = await GetTrackedPostWithDetailsAsync(id);

        var originalTitle = post.Title;
        var wasPublished = post.IsPublished;
        var previousImage = post.FeaturedImageUrls;

        // ApplyTo resolves the featured image too - the posted value is the large URL when
        // untouched and a fresh JSON blob after an upload.
        viewModel.ApplyTo(post);

        if (originalTitle != viewModel.Title)
        {
            // Changing slugs on published posts breaks external links.
            if (!wasPublished)
            {
                post.Slug = SlugHelper.GenerateSlug(viewModel.Title);
                post.Slug = await EnsureUniqueSlugAsync(post.Slug, post.Id);
            }
        }

        if (viewModel.PublishNow && post.PublishedDate == null)
        {
            // Only stamp the first publication. Re-publishing after an unpublish must keep
            // the original date, or an old post jumps back to the top of the blog.
            post.PublishedDate = DateTime.UtcNow;
        }

        var desiredTagIds = viewModel.SelectedTagIds.ToHashSet();
        var currentTagIds = post.PostTags.Select(pt => pt.TagId).ToHashSet();

        _db.PostTags.RemoveRange(post.PostTags.Where(pt => !desiredTagIds.Contains(pt.TagId)));
        foreach (var tagId in desiredTagIds.Except(currentTagIds))
        {
            _db.PostTags.Add(new PostTag { PostId = post.Id, TagId = tagId });
        }

        await _db.SaveChangesAsync();

        // Only after the row is safely persisted, so a failed save never orphans the live image.
        if (!string.Equals(previousImage, post.FeaturedImageUrls, StringComparison.Ordinal))
        {
            _attachmentService.DeleteStoredImages(previousImage);
        }
    }

    private async Task<Post> GetTrackedPostWithDetailsAsync(int id)
    {
        return await _db.Posts
            .IncludingDeleted()
            .WithDetails()
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Post with ID {id} not found");
    }

    public async Task DeletePostAsync(int id)
    {
        var post = await GetTrackedPostWithDetailsAsync(id);
        var image = post.FeaturedImageUrls;

        _db.PostTags.RemoveRange(post.PostTags);
        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();

        _attachmentService.DeleteStoredImages(image);
    }

    public async Task<bool> PostExistsAsync(int id)
    {
        return await _db.Posts.AnyAsync(p => p.Id == id);
    }

    public async Task<List<SelectListItem>> GetAvailableTagsAsync()
    {
        return await _tagService.GetAvailableTagsAsync();
    }

    public async Task PublishPostAsync(int id)
    {
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Post with ID {id} not found");

        post.IsPublished = true;
        // Preserve the original publication date across unpublish/republish cycles.
        post.PublishedDate ??= DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task UnpublishPostAsync(int id)
    {
        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Post with ID {id} not found");

        post.IsPublished = false;

        await _db.SaveChangesAsync();
    }

    public async Task SoftDeletePostAsync(int id)
    {
        var post = await _db.Posts.IncludingDeleted().FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Post with ID {id} not found");

        post.IsDeleted = true;
        post.DeletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    public async Task RestorePostAsync(int id)
    {
        var post = await _db.Posts.IncludingDeleted().FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Post with ID {id} not found");

        post.IsDeleted = false;
        post.DeletedAt = null;

        await _db.SaveChangesAsync();
    }

    public async Task<DataTablesResponse<PostViewModel>> GetPostListForDataTableAsync(DataTablesRequest request)
    {
        var includeDeleted = request.StatusFilter is "trash" or "Trashed" or "All";
        IQueryable<Post> query = includeDeleted ? _db.Posts.IncludingDeleted() : _db.Posts;

        // Apply status filter
        query = request.StatusFilter?.ToLower() switch
        {
            "published" => query.Where(p => p.IsPublished && !p.IsDeleted),
            "draft" => query.Where(p => !p.IsPublished && !p.IsDeleted),
            "trash" or "trashed" => query.Where(p => p.IsDeleted),
            "all" => query,
            "active" => query.Where(p => !p.IsDeleted),
            _ => query.Where(p => !p.IsDeleted)
        };

        var totalForStatus = await query.CountAsync();

        // Apply search
        if (!string.IsNullOrWhiteSpace(request.Search?.Value))
        {
            var searchTerm = request.Search.Value.ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(searchTerm) ||
                (p.Content != null && p.Content.ToLower().Contains(searchTerm)));
        }

        var filteredCount = await query.CountAsync();

        // Apply ordering
        if (request.Order.Count > 0 &&
            request.Order[0].Column >= 0 &&
            request.Order[0].Column < request.Columns.Count)
        {
            var orderColumn = request.Columns[request.Order[0].Column].Data;
            var orderDir = request.Order[0].Dir == "asc" ? "asc" : "desc";

            query = orderColumn switch
            {
                "title" => orderDir == "asc" ? query.OrderBy(p => p.Title) : query.OrderByDescending(p => p.Title),
                "publishedDate" => orderDir == "asc" ? query.OrderBy(p => p.PublishedDate) : query.OrderByDescending(p => p.PublishedDate),
                "createdAt" => orderDir == "asc" ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };
        }
        else
        {
            query = query.OrderByDescending(p => p.CreatedAt);
        }

        // Apply pagination and include related data
        var posts = await query
            .Skip(request.Start)
            .Take(request.Length)
            .Include(p => p.Category)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Include(p => p.Author)
            .ToListAsync();

        var viewModels = posts.Select(post => new PostViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Excerpt = MarkdownExcerpt.ToPlainText(post.Content, 100),
            CategoryName = post.Category?.Name ?? "Uncategorized",
            Tags = post.PostTags.Select(pt => pt.Tag?.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList(),
            AuthorName = post.Author?.UserName ?? "Unknown",
            PublishedDate = post.PublishedDate,
            CreatedAt = post.CreatedAt,
            IsPublished = post.IsPublished,
            IsDeleted = post.IsDeleted,
            DeletedAt = post.DeletedAt,
            FeaturedImageUrl = FeaturedImage.GetThumbnailUrl(post.FeaturedImageUrls)
        }).ToList();

        return new DataTablesResponse<PostViewModel>
        {
            Draw = request.Draw,
            RecordsTotal = totalForStatus,
            RecordsFiltered = filteredCount,
            Data = viewModels
        };
    }

    private async Task<string> EnsureUniqueSlugAsync(string slug, int? postId = null)
    {
        // A title that normalises to nothing (e.g. non-Latin script) yields an empty slug,
        // which would then collide with every other such post on the unique index.
        var baseSlug = string.IsNullOrEmpty(slug) ? "post" : slug;

        var candidate = baseSlug;
        var counter = 1;

        // IX_Posts_Slug is unique across trashed rows too, but the global soft-delete filter
        // hides them - so uniqueness has to be checked against unfiltered rows or a trashed
        // post silently squats its slug and the insert fails with 23505.
        while (await _db.Posts
                   .IgnoreQueryFilters()
                   .AnyAsync(p => p.Slug == candidate && (!postId.HasValue || p.Id != postId.Value)))
        {
            var suffix = $"-{counter++}";
            var trimmed = SlugHelper.Truncate(baseSlug, SlugHelper.MaxSlugLength - suffix.Length);
            candidate = trimmed + suffix;
        }

        return candidate;
    }

    public async Task<int> EmptyTrashAsync()
    {
        var trashedPosts = await _db.Posts
            .IncludingDeleted()
            .Where(p => p.IsDeleted)
            .Include(p => p.PostTags)
            .ToListAsync();

        var images = trashedPosts.Select(p => p.FeaturedImageUrls).ToList();

        foreach (var post in trashedPosts)
        {
            _db.PostTags.RemoveRange(post.PostTags);
        }
        _db.Posts.RemoveRange(trashedPosts);
        await _db.SaveChangesAsync();

        foreach (var image in images)
        {
            _attachmentService.DeleteStoredImages(image);
        }

        return trashedPosts.Count;
    }

    /// <summary>
    /// Editor uploads are written straight to disk and only ever referenced from post
    /// markdown, so abandoning a draft leaks them. Sweeps files no post body mentions.
    /// </summary>
    public async Task<int> CleanupOrphanedEditorImagesAsync()
    {
        var content = await _db.Posts
            .IgnoreQueryFilters()
            .Where(p => p.Content != null)
            .Select(p => p.Content!)
            .ToListAsync();

        return _attachmentService.DeleteUnreferencedImages("posts/editor", content);
    }

    public async Task<int> RestoreAllPostsAsync()
    {
        var trashedPosts = await _db.Posts
            .IncludingDeleted()
            .Where(p => p.IsDeleted)
            .ToListAsync();

        foreach (var post in trashedPosts)
        {
            post.IsDeleted = false;
            post.DeletedAt = null;
        }
        await _db.SaveChangesAsync();

        return trashedPosts.Count;
    }
}