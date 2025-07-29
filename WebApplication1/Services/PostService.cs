using AutoMapper;
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
    private readonly IMapper _mapper;

    public PostService(
        IPostRepository postRepository,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        IUrlHelperFactory urlHelperFactory,
        IActionContextAccessor actionContextAccessor,
        IMapper mapper)
    {
        _postRepository = postRepository;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
        _mapper = mapper;
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

        return new BlogIndexViewModel
        {
            Posts = _mapper.Map<List<PostCardViewModel>>(postsData),
            Categories = _mapper.Map<List<CategoryViewModel>>(categories),
            Tags = _mapper.Map<List<TagViewModel>>(popularTags),
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
        if (post == null) return null;

        return _mapper.Map<PostViewModel>(post);
    }

    public async Task<EditPostViewModel?> GetPostForEditAsync(int id)
    {
        var post = await _postRepository.GetPostWithDetailsAsync(id);

        var viewModel = _mapper.Map<EditPostViewModel>(post);

        viewModel.AvailableTags = await GetAvailableTagsAsync();
        viewModel.AvailableCategories = (await _categoryRepository.GetAllAsync())
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToList();

        return viewModel;
    }

    public async Task<Post> CreatePostAsync(CreatePostViewModel viewModel, string userId)
    {
        var post = _mapper.Map<Post>(viewModel);
        post.CreatedAt = DateTime.UtcNow;
        post.PublishedDate = viewModel.PublishNow ? DateTime.UtcNow : null;
        post.AuthorId = userId;
        post.Slug = SlugHelper.GenerateSlug(viewModel.Title);
        post.CategoryId = viewModel.CategoryId == 0 ? null : viewModel.CategoryId;
        post.Content = SanitizeContent(viewModel.Content);

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

        var originalTitle = post.Title;
        var wasPublished = post.IsPublished;

        _mapper.Map(viewModel, post);
        if (originalTitle != viewModel.Title)
        {
            post.Slug = SlugHelper.GenerateSlug(viewModel.Title);
            post.Slug = await EnsureUniqueSlugAsync(post.Slug, post.Id);
        }

        post.Content = SanitizeContent(viewModel.Content);

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

        var postViewModels = _mapper.Map<List<PostViewModel>>(posts);

        var postsList = posts.ToList();

        foreach (var vm in postViewModels)
        {
            var post = postsList.First(p => p.Id == vm.Id);
            vm.Status = post.IsDeleted
                ? "<span class='badge bg-danger'>In Trash</span>"
                : post.IsPublished
                    ? "<span class='badge bg-success'>Published</span>"
                    : "<span class='badge bg-secondary'>Draft</span>";

            if (_urlHelper == null) continue;
            var editUrl = _urlHelper.Action("Edit", "Post", new { id = vm.Id, area = "Admin" });
            var deleteUrl = _urlHelper.Action("SoftDelete", "Post", new { id = vm.Id, area = "Admin" });
            var restoreUrl = _urlHelper.Action("Restore", "Post", new { id = vm.Id, area = "Admin" });

            var actionsHtml = $"<div class='btn-group' role='group'><a href='{editUrl}' class='btn btn-sm btn-outline-primary' title='Edit'><i class='bi bi-pencil'></i></a>";
            if (post.IsDeleted)
            {
                actionsHtml += $"<form method='post' action='{restoreUrl}' class='d-inline restore-form'><button type='submit' class='btn btn-sm btn-outline-success' title='Restore'><i class='bi bi-arrow-counterclockwise'></i></button></form>";
            }
            else
            {
                actionsHtml += $"<form method='post' action='{deleteUrl}' class='d-inline delete-form' data-post-title='{post.Title}'><button type='submit' class='btn btn-sm btn-outline-danger' title='Delete'><i class='bi bi-trash'></i></button></form>";
            }
            actionsHtml += "</div>";
            vm.Actions = actionsHtml;
        }

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