using Ganss.Xss;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebApplication1.Areas.Admin.ViewModels.Post;
using WebApplication1.Interfaces;
using WebApplication1.Models;
using WebApplication1.Utils;
using WebApplication1.ViewModels;

namespace WebApplication1.Services;

public class PostService : IPostService
{
    private readonly IPostRepository _postRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITagRepository _tagRepository;

    public PostService(
        IPostRepository postRepository,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository)
    {
        _postRepository = postRepository;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
    }

    public async Task<BlogIndexViewModel> GetBlogIndexViewModelAsync(int page, string? category, string? tag)
    {
        const int pageSize = 5;
        var skip = (page - 1) * pageSize;

        var posts = await _postRepository.GetPublishedPostsAsync(skip, pageSize);
        var totalPosts = await _postRepository.CountPublishedPostsAsync();

        var categories = await _categoryRepository.GetAllAsync();
        var tags = await _tagRepository.GetPopularTagsAsync(10);

        return new BlogIndexViewModel
        {
            Posts = posts.ToList(),
            Categories = categories.ToList(),
            Tags = tags.ToList(),
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(totalPosts / (double)pageSize),
            CurrentCategory = category,
            CurrentTag = tag
        };
    }

    public async Task<Post?> GetPostByIdAsync(int id, bool includeUnpublished = false)
    {
        try
        {
            var post = await _postRepository.GetPostWithDetailsAsync(id);
            if (!includeUnpublished && !post.IsPublished)
            {
                return null;
            }
            return post;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    public async Task<PostListViewModel> GetPostListAsync(int page, string? searchTerm, string? tagFilter, bool? publishedOnly)
    {
        const int pageSize = 10;
        var skip = (page - 1) * pageSize;

        var posts = await _postRepository.GetPostsWithFiltersAsync(searchTerm, tagFilter, publishedOnly, skip, pageSize);
        var totalPosts = await _postRepository.CountPostsWithFiltersAsync(searchTerm, tagFilter, publishedOnly);

        var postSummaries = posts.Select(p => new PostSummaryViewModel
        {
            Id = p.Id,
            Title = p.Title,
            CreatedAt = p.CreatedAt,
            IsPublished = p.IsPublished,
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
            SelectedTagIds = post.PostTags.Select(pt => pt.TagId).ToList(),
            AvailableTags = await GetAvailableTagsAsync()
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
            CategoryId = viewModel.CategoryId
        };

        post.Slug = await EnsureUniqueSlugAsync(post.Slug);

        await _postRepository.AddAsync(post);
        await _postRepository.SaveChangesAsync();

        if (viewModel.SelectedTagIds.Count == 0) return post;
        foreach (var tagId in viewModel.SelectedTagIds)
        {
            await _postRepository.AddPostTagAsync(new PostTag
            {
                PostId = post.Id,
                TagId = tagId
            });
        }
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

        if (!post.IsPublished && viewModel.IsPublished)
        {
            post.PublishedDate = DateTime.UtcNow;
        }
        post.IsPublished = viewModel.IsPublished;

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
        var post = await _postRepository.GetByIdAsync(id);

        if (post == null)
            throw new KeyNotFoundException($"Post with ID {id} not found");

        post.IsDeleted = true;
        post.DeletedAt = DateTime.UtcNow;

        await _postRepository.UpdateAsync(post);
        await _postRepository.SaveChangesAsync();
    }

    public async Task RestorePostAsync(int id)
    {
        var post = await _postRepository.GetByIdAsync(id);

        if (post == null)
            throw new KeyNotFoundException($"Post with ID {id} not found");

        post.IsDeleted = false;
        post.DeletedAt = null;

        await _postRepository.UpdateAsync(post);
        await _postRepository.SaveChangesAsync();
    }
}