using System.Text.Json;
using AutoMapper;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Admin.ViewModels.Post;
using WebApp.Helpers;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.Utils;
using WebApp.ViewModels;

namespace WebApp.Services;

public class PostService : IPostService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IPostRepository _postRepository;
    private readonly ITagRepository _tagRepository;
    private readonly LinkGenerator _linkGenerator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMapper _mapper;
    private readonly ITagService _tagService;

    public PostService(
        IPostRepository postRepository,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        LinkGenerator linkGenerator,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        ITagService tagService)
    {
        _postRepository = postRepository;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
        _linkGenerator = linkGenerator;
        _httpContextAccessor = httpContextAccessor;
        _mapper = mapper;
        _tagService = tagService;
    }

    public async Task<BlogIndexViewModel> GetBlogIndexViewModelAsync(int page, string? category, string? tag)
    {
        const int pageSize = 5;
        var skip = (page - 1) * pageSize;

        var postsData = await _postRepository.GetPublishedPostsWithDetailsAsync(skip, pageSize, category, tag);
        var totalPosts = await _postRepository.CountPublishedPostsAsync();

        var categories = await _categoryRepository.GetAllAsync();
        var popularTags = await _tagRepository.GetPopularTagsAsync(10);

        return new BlogIndexViewModel
        {
            Posts = _mapper.Map<List<PostCardViewModel>>(postsData),
            Categories = _mapper.Map<List<CategoryViewModel>>(categories),
            Tags = _mapper.Map<List<TagViewModel>>(popularTags),
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize),
            CurrentCategory = category,
            CurrentTag = tag,
            Category = category,
            Tag = tag
        };
    }

    public async Task<Post?> GetPostByIdAsync(int id, bool includeUnpublished = false, bool includeDeleted = false)
    {
        return await _postRepository.GetByIdAsync(id, includeUnpublished, includeDeleted);
    }

    public async Task<PostListViewModel> GetPostListAsync(int page, string? searchTerm, string? tagFilter,
        bool? publishedOnly)
    {
        const int pageSize = 10;
        var skip = (page - 1) * pageSize;

        var posts = await _postRepository.GetPostsWithFiltersAsync(searchTerm, tagFilter, publishedOnly, skip,
            pageSize);
        var totalPosts = await _postRepository.CountPostsWithFiltersAsync(searchTerm, tagFilter, publishedOnly);

        var postSummaries = _mapper.Map<List<PostSummaryViewModel>>(posts);

        return new PostListViewModel
        {
            Posts = postSummaries,
            TotalPosts = totalPosts,
            CurrentPage = page,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            ShowPublishedOnly = publishedOnly ?? false
        };
    }

    public async Task<PostViewModel?> GetPostViewModelAsync(int id)
    {
        var post = await _postRepository.GetPostWithDetailsAsync(id);
        return _mapper.Map<PostViewModel>(post);
    }

    public async Task<PostViewModel?> GetPostViewModelBySlugAsync(string slug)
    {
        var post = await _postRepository.GetBySlugAsync(slug);
        return _mapper.Map<PostViewModel>(post);
    }

    public async Task<EditPostViewModel?> GetPostForEditAsync(int id)
    {
        var post = await _postRepository.GetPostWithDetailsAsync(id);
        if (post == null) return null;

        var viewModel = _mapper.Map<EditPostViewModel>(post);

        viewModel.AvailableTags = await GetAvailableTagsAsync();
        viewModel.AvailableCategories = (await _categoryRepository.GetAllAsync())
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();

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

        await _postRepository.AddAsync(post);
        await _postRepository.SaveChangesAsync();

        if (viewModel.SelectedTagIds is not { Count: > 0 }) return post;
        foreach (var tagId in viewModel.SelectedTagIds)
        {
            var postTag = new PostTag { PostId = post.Id, TagId = tagId };
            await _postRepository.AddPostTagAsync(postTag);
        }
        await _postRepository.SaveChangesAsync();

        return post;
    }

    public async Task UpdatePostAsync(int id, EditPostViewModel viewModel)
    {
        var post = await _postRepository.GetPostWithDetailsAsync(id);

        var originalTitle = post.Title;
        var wasPublished = post.IsPublished;

        _mapper.Map(viewModel, post);

        // 1. Image Logic: Preserve JSON if the image hasn't changed
        if (viewModel.FeaturedImageUrl != GetStoredLargeUrl(post.FeaturedImageUrls))
            post.FeaturedImageUrls = viewModel.FeaturedImageUrl;

        // 2. Slug Logic: Protect SEO
        if (originalTitle != viewModel.Title)
        {
            // Only auto-update slug if the post is NOT published yet.
            // Changing slugs on published posts breaks external links.
            if (!wasPublished)
            {
                post.Slug = SlugHelper.GenerateSlug(viewModel.Title);
                post.Slug = await EnsureUniqueSlugAsync(post.Slug, post.Id);
            }
        }

        post.Content = viewModel.Content;

        if (!wasPublished && viewModel.PublishNow)
        {
            post.PublishedDate = DateTime.UtcNow;
        }
        post.IsPublished = viewModel.PublishNow;

        await _postRepository.DeletePostTagsAsync(post.PostTags);
        if (viewModel.SelectedTagIds.Count != 0)
        {
            foreach (var tagId in viewModel.SelectedTagIds)
            {
                await _postRepository.AddPostTagAsync(new PostTag
                {
                    PostId = post.Id,
                    TagId = tagId
                });
            }
        }

        await _postRepository.UpdateAsync(post);
        await _postRepository.SaveChangesAsync();
    }

    public async Task DeletePostAsync(int id)
    {
        var post = await _postRepository.GetPostWithDetailsAsync(id);
        await _postRepository.DeletePostTagsAsync(post.PostTags);
        await _postRepository.DeleteAsync(post);
        await _postRepository.SaveChangesAsync();
    }

    public async Task<bool> PostExistsAsync(int id)
    {
        return await _postRepository.PostExistsAsync(id);
    }

    public async Task<List<SelectListItem>> GetAvailableTagsAsync()
    {
        return await _tagService.GetAvailableTagsAsync();
    }

    public async Task PublishPostAsync(int id)
    {
        var post = await _postRepository.GetByIdAsync(id);

        if (post == null)
            throw new KeyNotFoundException($"Post with ID {id} not found");

        post.IsPublished = true;
        post.PublishedDate = DateTime.UtcNow;

        await _postRepository.UpdateAsync(post);
        await _postRepository.SaveChangesAsync();
    }

    public async Task UnpublishPostAsync(int id)
    {
        var post = await _postRepository.GetByIdAsync(id);

        if (post == null)
            throw new KeyNotFoundException($"Post with ID {id} not found");

        post.IsPublished = false;

        await _postRepository.UpdateAsync(post);
        await _postRepository.SaveChangesAsync();
    }

    public async Task SoftDeletePostAsync(int id)
    {
        var post = await _postRepository.GetPostWithDetailsAsync(id);

        if (post == null)
            throw new KeyNotFoundException($"Post with ID {id} not found");

        post.IsDeleted = true;
        post.DeletedAt = DateTime.UtcNow;

        await _postRepository.UpdateAsync(post);
        await _postRepository.SaveChangesAsync();
    }

    public async Task RestorePostAsync(int id)
    {
        var post = await _postRepository.GetPostWithDetailsAsync(id);

        if (post == null)
            throw new KeyNotFoundException($"Post with ID {id} not found");

        post.IsDeleted = false;
        post.DeletedAt = null;

        await _postRepository.UpdateAsync(post);
        await _postRepository.SaveChangesAsync();
    }

    public async Task<DataTablesResponse<PostViewModel>> GetPostListForDataTableAsync(DataTablesRequest request)
    {
        var includeDeleted = request.StatusFilter is "trash" or "Trashed" or "All";
        var query = _postRepository.GetQueryable(includeDeleted);

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
            Excerpt = GetPlainTextExcerpt(post.Content, 100),
            CategoryName = post.Category?.Name ?? "Uncategorized",
            Tags = post.PostTags.Select(pt => pt.Tag?.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList(),
            AuthorName = post.Author?.UserName ?? "Unknown",
            PublishedDate = post.PublishedDate,
            CreatedAt = post.CreatedAt,
            IsPublished = post.IsPublished,
            IsDeleted = post.IsDeleted,
            FeaturedImageUrl = GetThumbnailUrl(post.FeaturedImageUrls)
        }).ToList();

        return new DataTablesResponse<PostViewModel>
        {
            Draw = request.Draw,
            RecordsTotal = totalForStatus,
            RecordsFiltered = filteredCount,
            Data = viewModels
        };
    }

    private static string GetPlainTextExcerpt(string? markdown, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var plainText = Markdig.Markdown.ToPlainText(markdown);
        plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"\s+", " ").Trim();

        if (plainText.Length <= maxLength)
            return plainText;

        var lastSpace = plainText.LastIndexOf(' ', maxLength);
        if (lastSpace > maxLength / 2)
            return plainText[..lastSpace] + "...";

        return plainText[..maxLength] + "...";
    }

    private static string? GetStoredLargeUrl(string? featuredImageUrls)
    {
        if (string.IsNullOrEmpty(featuredImageUrls)) return null;
        if (!featuredImageUrls.TrimStart().StartsWith('{')) return featuredImageUrls;
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(featuredImageUrls);
            return dict?.GetValueOrDefault("large");
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetThumbnailUrl(string? featuredImageUrls)
    {
        if (string.IsNullOrEmpty(featuredImageUrls))
            return null;

        try
        {
            var urls = JsonSerializer.Deserialize<Dictionary<string, string>>(featuredImageUrls);
            return urls?.GetValueOrDefault("thumbnail") ?? urls?.Values.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> EnsureUniqueSlugAsync(string slug, int? postId = null)
    {
        var originalSlug = slug;
        var currentSlug = slug;
        var counter = 1;

        while (await _postRepository.SlugExistsAsync(currentSlug, postId))
        {
            currentSlug = $"{originalSlug}-{counter}";
            counter++;
        }

        return currentSlug;
    }

    public async Task EmptyTrashAsync()
    {
        var trashedPosts = await _postRepository.GetAllTrashedPostsAsync();
        foreach (var post in trashedPosts)
        {
            await _postRepository.DeletePostTagsAsync(post.PostTags);
            await _postRepository.DeleteAsync(post);
        }
        await _postRepository.SaveChangesAsync();
    }
    
    public async Task RestoreAllPostsAsync()
    {
        var trashedPosts = await _postRepository.GetAllTrashedPostsAsync();
        foreach (var post in trashedPosts)
        {
            post.IsDeleted = false;
            post.DeletedAt = null;
            await _postRepository.UpdateAsync(post);
        }
        await _postRepository.SaveChangesAsync();
    }

    public async Task<Post?> GetPostBySlugAsync(string slug)
    {
        return await _postRepository.GetBySlugAsync(slug);
    }

    public async Task<Tag> CreateTagAsync(string name)
    {
        var tag = new Tag { Name = name.Trim() };
        await _tagService.CreateTagAsync(tag);
        return tag;
    }
}