using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Areas.Admin.ViewModels.Posts;
using WebApplication1.Data;
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
    private readonly ApplicationDbContext _context;

    public PostService(
        IPostRepository postRepository,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        ApplicationDbContext context)
    {
        _postRepository = postRepository;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
        _context = context;
    }

    public async Task<BlogIndexViewModel> GetBlogIndexViewModelAsync(int page, string? category, string? tag)
    {
        int pageSize = 5;
        int skip = (page - 1) * pageSize;

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

    public async Task<Post?> GetPostByIdAsync(int id)
    {
        return await _postRepository.GetPostWithDetailsAsync(id);
    }

    public async Task<PostListViewModel> GetPostListAsync(int page, string? searchTerm, string? tagFilter, bool? publishedOnly)
    {
        int pageSize = 10;
        var postsQuery = _context.Posts
            .Include(p => p.Author)
            .Include(p => p.PostTags)
            .ThenInclude(pt => pt.Tag)
            .AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            postsQuery = postsQuery.Where(p => p.Title.Contains(searchTerm) || p.Content!.Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(tagFilter))
        {
            postsQuery = postsQuery.Where(p => p.PostTags.Any(pt => pt.Tag!.Name == tagFilter));
        }

        if (publishedOnly.HasValue && publishedOnly.Value)
        {
            postsQuery = postsQuery.Where(p => p.IsPublished);
        }

        int totalPosts = await postsQuery.CountAsync();

        var posts = await postsQuery
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PostSummaryViewModel
            {
                Id = p.Id,
                Title = p.Title,
                CreatedAt = p.CreatedAt,
                IsPublished = p.IsPublished,
                AuthorName = p.Author!.DisplayName ?? p.Author.UserName ?? "Unknown",
                Tags = p.PostTags.Select(pt => pt.Tag!.Name).ToList()
            }).ToListAsync();

        return new PostListViewModel
        {
            Posts = posts,
            TotalPosts = totalPosts,
            CurrentPage = page,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            ShowPublishedOnly = publishedOnly ?? false
        };
    }

    public async Task<PostViewModel?> GetPostViewModelAsync(int id)
    {
        var post = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.PostTags)
            .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
            return null;

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
        var post = await _context.Posts
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
            return null;

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

    public async Task<Post> CreatePostAsync(CreatePostViewModel viewModel, string userId)
    {
        var post = new Post
        {
            Title = viewModel.Title,
            Content = viewModel.Content,
            CreatedAt = DateTime.UtcNow,
            IsPublished = viewModel.PublishNow,
            PublishedDate = viewModel.PublishNow ? DateTime.UtcNow : null,
            AuthorId = userId,
            Slug = SlugHelper.GenerateSlug(viewModel.Title)
        };

        post.Slug = await EnsureUniqueSlugAsync(post.Slug);

        await _postRepository.AddAsync(post);
        await _postRepository.SaveChangesAsync();

        if (viewModel.SelectedTagIds.Any())
        {
            foreach (var tagId in viewModel.SelectedTagIds)
            {
                _context.PostTags.Add(new PostTag
                {
                    PostId = post.Id,
                    TagId = tagId
                });
            }
            await _context.SaveChangesAsync();
        }

        return post;
    }

    public async Task UpdatePostAsync(int id, EditPostViewModel viewModel)
    {
        var post = await _context.Posts
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
            throw new KeyNotFoundException($"Post with ID {id} not found");

        if (post.Title != viewModel.Title)
        {
            post.Slug = SlugHelper.GenerateSlug(viewModel.Title);
            post.Slug = await EnsureUniqueSlugAsync(post.Slug, post.Id);
        }

        post.Title = viewModel.Title;
        post.Content = viewModel.Content;

        if (!post.IsPublished && viewModel.IsPublished)
        {
            post.PublishedDate = DateTime.UtcNow;
        }
        post.IsPublished = viewModel.IsPublished;

        _context.PostTags.RemoveRange(post.PostTags);

        if (viewModel.SelectedTagIds.Any())
        {
            foreach (var tagId in viewModel.SelectedTagIds)
            {
                _context.PostTags.Add(new PostTag
                {
                    PostId = post.Id,
                    TagId = tagId
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeletePostAsync(int id)
    {
        var post = await _context.Posts
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
            return;

        _context.PostTags.RemoveRange(post.PostTags);
        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> PostExistsAsync(int id)
    {
        return await _context.Posts.AnyAsync(e => e.Id == id);
    }

    public async Task<List<SelectListItem>> GetAvailableTagsAsync()
    {
        return await _context.Tags
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.Name
            }).ToListAsync();
    }

    private async Task<string> EnsureUniqueSlugAsync(string slug, int? postId = null)
    {
        var originalSlug = slug;
        var counter = 1;

        while (await _context.Posts.AnyAsync(p =>
            p.Slug == slug && (!postId.HasValue || p.Id != postId)))
        {
            slug = $"{originalSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    public async Task PublishPostAsync(int id)
    {
        var post = await _context.Posts.FindAsync(id);

        if (post == null)
            throw new KeyNotFoundException($"Post with ID {id} not found");

        post.IsPublished = true;
        post.PublishedDate = DateTime.UtcNow;

        _context.Update(post);
        await _context.SaveChangesAsync();
    }

    public async Task UnpublishPostAsync(int id)
    {
        var post = await _context.Posts.FindAsync(id);

        if (post == null)
            throw new KeyNotFoundException($"Post with ID {id} not found");

        post.IsPublished = false;

        _context.Update(post);
        await _context.SaveChangesAsync();
    }
}