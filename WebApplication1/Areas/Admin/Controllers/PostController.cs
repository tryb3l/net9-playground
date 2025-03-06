using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Areas.Admin.ViewModels.Posts;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Utils;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class PostController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public PostController(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int page = 1,
        string? searchTerm = null, string? tagFilter = null,
        bool? publishedOnly = null)
    {
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

        int pageSize = 10;

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

        var totalPosts = await postsQuery.CountAsync();

        var viewModel = new PostListViewModel
        {
            Posts = posts,
            TotalPosts = totalPosts,
            CurrentPage = page,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            ShowPublishedOnly = publishedOnly ?? false
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var post = await _context.Posts
        .Include(p => p.Author)
        .Include(p => p.PostTags)
        .ThenInclude(pt => pt.Tag)
        .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
        {
            return NotFound();
        }

        var viewModel = new PostViewModel
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
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePostViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var post = new Post
            {
                Title = viewModel.Title,
                Content = viewModel.Content,
                CreatedAt = DateTime.UtcNow,
                IsPublished = viewModel.PublishNow,
                PublishedDate = viewModel.PublishNow ? DateTime.UtcNow : null,
                AuthorId = currentUser?.Id,
                Slug = SlugHelper.GenerateSlug(viewModel.Title)
            };

            post.Slug = await EnsureUniqueSlug(post.Slug);

            _context.Add(post);
            await _context.SaveChangesAsync();

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
            return RedirectToAction(nameof(Index));
        }
        viewModel.AvailableTags = await GetAvailableTags();
        return View(viewModel);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var post = await _context.Posts
        .Include(p => p.PostTags)
        .FirstOrDefaultAsync(p => p.Id == id);

        if (post == null)
        {
            return NotFound();
        }

        var viewModel = new EditPostViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            IsPublished = post.IsPublished,
            PublishedDate = post.PublishedDate,
            CreatedAt = post.CreatedAt,
            SelectedTagIds = post.PostTags.Select(pt => pt.TagId).ToList(),
            AvailableTags = await GetAvailableTags()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditPostViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var post = await _context.Posts
                    .Include(p => p.PostTags)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (post == null)
                {
                    return NotFound();
                }

                if (post.Title != viewModel.Title)
                {
                    post.Slug = SlugHelper.GenerateSlug(viewModel.Title);
                    post.Slug = await EnsureUniqueSlug(post.Slug, post.Id);
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
                _context.Update(post);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PostExists(viewModel.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        viewModel.AvailableTags = await GetAvailableTags();
        return View(viewModel);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var post = await _context.Posts
        .Include(p => p.Author)
        .Include(p => p.PostTags)
        .ThenInclude(pt => pt.Tag)
        .FirstOrDefaultAsync(m => m.Id == id);

        if (post == null)
        {
            return NotFound();
        }

        var viewModel = new PostViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            CreatedAt = post.CreatedAt,
            PublishedDate = post.PublishedDate,
            IsPublished = post.IsPublished,
            AuthorName = post.Author?.DisplayName ?? post.Author?.UserName ?? "Unknown",
            Tags = post.PostTags.Select(pt => pt.Tag!.Name).ToList()
        };

        return View(viewModel);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var post = await _context.Posts
        .Include(p => p.PostTags)
        .FirstOrDefaultAsync(p => p.Id == id);

        if (post != null)
        {
            _context.PostTags.RemoveRange(post.PostTags);
            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private bool PostExists(int id)
    {
        return _context.Posts.Any(e => e.Id == id);
    }

    private async Task<List<SelectListItem>> GetAvailableTags()
    {
        return await _context.Tags
        .OrderBy(t => t.Name)
        .Select(t => new SelectListItem
        {
            Value = t.Id.ToString(),
            Text = t.Name
        }).ToListAsync();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        var post = await _context.Posts
        .FindAsync(id);

        if (post == null)
        {
            return NotFound();
        }

        post.IsPublished = true;
        post.PublishedDate = DateTime.UtcNow;

        _context.Update(post);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(int id)
    {
        var post = await _context.Posts.FindAsync(id);

        if (post == null)
        {
            return NotFound();
        }

        post.IsPublished = false;

        _context.Update(post);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<string> EnsureUniqueSlug(string slug, int? postId = null)
    {
        var originalSlug = slug;
        var counter = 1;

        // Check if slug is already used by another post
        while (await _context.Posts.AnyAsync(p =>
            p.Slug == slug && (!postId.HasValue || p.Id != postId)))
        {
            // Add a numeric suffix to make it unique
            slug = $"{originalSlug}-{counter}";
            counter++;
        }

        return slug;
    }
}