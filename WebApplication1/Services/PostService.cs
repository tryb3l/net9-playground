using Ganss.Xss;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using WebApplication1.Areas.Admin.ViewModels.Post;
using WebApplication1.Helpers;
using WebApplication1.Interfaces;
using WebApplication1.Models;
using WebApplication1.Utils;
using WebApplication1.ViewModels;

namespace WebApplication1.Services;

public class PostService : IPostService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IPostRepository _postRepository;
    private readonly ITagRepository _tagRepository;
    private readonly IUrlHelper? _urlHelper;

    public PostService(
        IPostRepository postRepository,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        IUrlHelperFactory urlHelperFactory,
        IActionContextAccessor actionContextAccessor)
    {
        _postRepository = postRepository;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
        if (actionContextAccessor.ActionContext != null)
            _urlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
    }

    public async Task<BlogIndexViewModel> GetBlogIndexViewModelAsync(int page, string? category, string? tag)
    {
        const int pageSize = 5;
        var skip = (page - 1) * pageSize;

        var postsData = await _postRepository.GetPublishedPostsWithDetailsAsync(skip, pageSize);
        var totalPosts = await _postRepository.CountPublishedPostsAsync();

        var categories = await _categoryRepository.GetAllAsync();
        var popularTags = await _tagRepository.GetPopularTagsAsync(10);

        var postCards = postsData.Select(p => new PostCardViewModel
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug ?? string.Empty,
            ShortDescription = !string.IsNullOrEmpty(p.Content) && p.Content.Length > 200
                ? p.Content.Substring(0, 200) + "..."
                : p.Content ?? string.Empty,
            PublishDate = p.PublishedDate ?? p.CreatedAt,
            CategoryName = p.Category?.Name ?? "Uncategorized",
            CategorySlug = p.Category?.Slug ?? string.Empty,
            Tags = p.PostTags.Select(pt => new TagViewModel
            {
                Name = pt.Tag!.Name,
                Slug = SlugHelper.GenerateSlug(pt.Tag!.Name)
            })
        }).ToList();

        return new BlogIndexViewModel
        {
            Posts = postCards,
            Categories = categories.Select(c => new CategoryViewModel
            {
                Name = c.Name,
                Slug = c.Slug ?? SlugHelper.GenerateSlug(c.Name),
                PostCount = c.Posts?.Count(p => p.IsPublished && !p.IsDeleted) ?? 0
            }).ToList(),
            Tags = popularTags.Select(t => new TagViewModel
            {
                Name = t.Name,
                Slug = SlugHelper.GenerateSlug(t.Name),
                PostCount = t.PostTags?.Count(pt => pt.Post != null && pt.Post.IsPublished && !pt.Post.IsDeleted) ?? 0
            }).ToList(),
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize),
            CurrentCategory = category,
            CurrentTag = tag
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

        var postSummaries = posts.Select(p => new PostSummaryViewModel
        {
            Id = p.Id,
            Title = p.Title,
            CreatedAt = p.CreatedAt,
            IsPublished = p.IsPublished,
            IsDeleted = p.IsDeleted,
            PublishedDate = p.PublishedDate,
            AuthorName = p.Author!.DisplayName ?? p.Author.UserName ?? "Unknown",
            Tags = p.PostTags.Select(pt => pt.Tag!.Name).ToList()
        }).ToList();

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

        return new PostViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            CreatedAt = post.CreatedAt,
            PublishedDate = post.PublishedDate,
            IsPublished = post.IsPublished,
            AuthorName = post.Author?.DisplayName ?? post.Author?.UserName ?? "Unknown",
            AuthorId = post.AuthorId ?? string.Empty,
            Tags = post.PostTags.Select(pt => pt.Tag!.Name).ToList()
        };
    }

    public async Task<EditPostViewModel?> GetPostForEditAsync(int id)
    {
        var post = await _postRepository.GetPostWithDetailsAsync(id);

        return new EditPostViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            IsPublished = post.IsPublished,
            PublishedDate = post.PublishedDate,
            CreatedAt = post.CreatedAt,
            CategoryId = post.CategoryId,
            SelectedTagIds = post.PostTags.Select(pt => pt.TagId).ToList(),
            AvailableTags = await GetAvailableTagsAsync(),
            AvailableCategories = (await _categoryRepository.GetAllAsync())
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList()
        };
    }

    public async Task<Post> CreatePostAsync(CreatePostViewModel viewModel, string userId)
    {
        var post = new Post
        {
            Title = viewModel.Title,
            Content = SanitizeContent(viewModel.Content),
            CreatedAt = DateTime.UtcNow,
            IsPublished = viewModel.PublishNow,
            PublishedDate = viewModel.PublishNow ? DateTime.UtcNow : null,
            AuthorId = userId,
            Slug = SlugHelper.GenerateSlug(viewModel.Title),
            CategoryId = viewModel.CategoryId == 0 ? null : viewModel.CategoryId
        };

        post.Slug = await EnsureUniqueSlugAsync(post.Slug);

        await _postRepository.AddAsync(post);
        await _postRepository.SaveChangesAsync();

        bool? any = viewModel.SelectedTagIds.Count != 0;

        if (any != true) return post;
        foreach (var postTag in viewModel.SelectedTagIds.Select(tagId => new PostTag
        {
            PostId = post.Id,
            TagId = tagId
        }))
            await _postRepository.AddPostTagAsync(postTag);
        await _postRepository.SaveChangesAsync();

        return post;
    }

    public async Task UpdatePostAsync(int id, EditPostViewModel viewModel)
    {
        var post = await _postRepository.GetPostWithDetailsAsync(id);

        if (post.Title != viewModel.Title)
        {
            post.Slug = SlugHelper.GenerateSlug(viewModel.Title);
            post.Slug = await EnsureUniqueSlugAsync(post.Slug, post.Id);
        }

        post.Title = viewModel.Title;
        post.Content = SanitizeContent(viewModel.Content);
        post.CategoryId = viewModel.CategoryId;

        if (post.IsDeleted)
        {
            post.IsDeleted = false;
            post.DeletedAt = null;
        }

        if (!post.IsPublished && viewModel.IsPublished) post.PublishedDate = DateTime.UtcNow;

        post.IsPublished = viewModel.IsPublished;

        await _postRepository.DeletePostTagsAsync(post.PostTags);

        if (viewModel.SelectedTagIds.Count != 0)
            foreach (var tagId in viewModel.SelectedTagIds)
                await _postRepository.AddPostTagAsync(new PostTag
                {
                    PostId = post.Id,
                    TagId = tagId
                });

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
        var tags = await _tagRepository.GetAllAsync();
        return tags
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.Name
            }).ToList();
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
        var searchTerm = request.Search?.Value;
        var order = request.Order.FirstOrDefault();
        var sortColumnIndex = order?.Column ?? 2;
        var sortColumnName = request.Columns.ElementAtOrDefault(sortColumnIndex)?.Name ?? "Created";
        var orderAsc = (order?.Dir ?? "desc") == "asc";

        var (posts, filteredCount, totalCount) = await _postRepository.GetPostsForDataTableAsync(
            request.Start,
            request.Length,
            searchTerm,
            sortColumnName,
            orderAsc,
            request.StatusFilter
        );

        var postViewModels = posts.Select(p =>
        {
            if (_urlHelper == null) return null;

            var editUrl = _urlHelper.Action("Edit", "Post", new { id = p.Id, area = "Admin" });
            var deleteUrl = _urlHelper.Action("SoftDelete", "Post", new { id = p.Id, area = "Admin" });
            var restoreUrl = _urlHelper.Action("Restore", "Post", new { id = p.Id, area = "Admin" });

            var actionsHtml = $"""
                                  <div class='btn-group' role='group'>
                                      <a href='{editUrl}' class='btn btn-sm btn-outline-primary' title='Edit'><i class='bi bi-pencil'></i></a>
                               """;

            if (p.IsDeleted)
            {
                actionsHtml += $"""
                                   <form method='post' action='{restoreUrl}' class='d-inline restore-form'>
                                       <button type='submit' class='btn btn-sm btn-outline-success' title='Restore'>
                                           <i class='bi bi-arrow-counterclockwise'></i>
                                       </button>
                                   </form>
                                """;
            }
            else
            {
                actionsHtml += $"""
                                    <form method='post' action='{deleteUrl}' class='d-inline delete-form' data-post-title='{p.Title}'>
                                        <button type='submit' class='btn btn-sm btn-outline-danger' title='Delete'>
                                            <i class='bi bi-trash'></i>
                                        </button>
                                    </form>
                                """;
            }

            actionsHtml += "</div>";

            return new PostViewModel
            {
                Id = p.Id,
                Title = p.Title,
                AuthorName = p.Author?.UserName ?? "N/A",
                CreatedAt = p.CreatedAt,
                PublishedDate = p.PublishedDate,
                IsPublished = p.IsPublished,
                IsDeleted = p.IsDeleted,
                Status = p.IsDeleted
                    ? "<span class='badge bg-danger'>In Trash</span>"
                    : p.IsPublished
                        ? "<span class='badge bg-success'>Published</span>"
                        : "<span class='badge bg-secondary'>Draft</span>",
                Actions = actionsHtml
            };
        })
        .Where(vm => vm != null)
        .ToList();

        return new DataTablesResponse<PostViewModel>
        {
            Draw = request.Draw,
            RecordsTotal = totalCount,
            RecordsFiltered = filteredCount,
            Data = postViewModels!
        };
    }

    private static string SanitizeContent(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        var sanitizer = new HtmlSanitizer();

        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.Add("h1");
        sanitizer.AllowedTags.Add("h2");
        sanitizer.AllowedTags.Add("h3");
        sanitizer.AllowedTags.Add("h4");
        sanitizer.AllowedTags.Add("p");
        sanitizer.AllowedTags.Add("a");
        sanitizer.AllowedTags.Add("ul");
        sanitizer.AllowedTags.Add("ol");
        sanitizer.AllowedTags.Add("li");
        sanitizer.AllowedTags.Add("strong");
        sanitizer.AllowedTags.Add("em");
        sanitizer.AllowedTags.Add("blockquote");
        sanitizer.AllowedTags.Add("code");
        sanitizer.AllowedTags.Add("img");
        sanitizer.AllowedTags.Add("br");

        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.Add("href");
        sanitizer.AllowedAttributes.Add("src");
        sanitizer.AllowedAttributes.Add("alt");
        sanitizer.AllowedAttributes.Add("title");

        return sanitizer.Sanitize(content);
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
        var posts = await _postRepository.GetAllAsync();
        return posts.FirstOrDefault(p => p.Slug == slug);
    }
}