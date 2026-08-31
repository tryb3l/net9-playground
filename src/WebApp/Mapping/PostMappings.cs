using WebApp.Areas.Admin.ViewModels.Dashboard;
using WebApp.Areas.Admin.ViewModels.Post;
using WebApp.Models;
using WebApp.Utils;

namespace WebApp.Mapping;

public static class PostMappings
{
    public static PostViewModel ToPostViewModel(this Post post) => new()
    {
        Id = post.Id,
        Title = post.Title,
        Slug = post.Slug,
        Excerpt = PostExcerpt.Resolve(post.Excerpt, post.Content),
        Content = post.Content,
        CategoryName = post.Category?.Name ?? "Uncategorized",
        CategorySlug = post.Category?.Slug,
        Tags = post.PostTags
            .Where(pt => pt.Tag != null)
            .Select(pt => new PostTagLinkViewModel { Name = pt.Tag!.Name, Slug = pt.Tag!.Slug ?? string.Empty })
            .ToList(),
        AuthorName = post.Author?.UserName ?? "N/A",
        PublishedDate = post.PublishedDate,
        CreatedAt = post.CreatedAt,
        IsPublished = post.IsPublished,
        IsDeleted = post.IsDeleted,
        DeletedAt = post.DeletedAt,
        FeaturedImageUrl = FeaturedImage.GetLargeUrl(post.FeaturedImageUrls),
        FeaturedImageAlt = post.FeaturedImageAlt
    };

    public static EditPostViewModel ToEditPostViewModel(this Post post) => new()
    {
        Id = post.Id,
        Slug = post.Slug,
        IsDeleted = post.IsDeleted,
        Title = post.Title,
        PublishNow = post.IsPublished,
        Content = post.Content,
        Excerpt = post.Excerpt,
        FeaturedImageUrl = FeaturedImage.GetLargeUrl(post.FeaturedImageUrls),
        FeaturedImageAlt = post.FeaturedImageAlt,
        IsPublished = post.IsPublished,
        PublishedDate = post.PublishedDate,
        CreatedAt = post.CreatedAt,
        CategoryId = post.CategoryId,
        SelectedTagIds = post.PostTags.Select(pt => pt.TagId).ToList()
    };

    public static RecentPostViewModel ToRecentPostViewModel(this Post post) => new()
    {
        Id = post.Id,
        Title = post.Title,
        CreatedAt = post.CreatedAt,
        UpdatedAt = post.UpdatedAt ?? post.CreatedAt,
        AuthorName = post.Author?.UserName ?? "N/A",
        IsPublished = post.IsPublished
    };

    public static void ApplyTo(this EditPostViewModel viewModel, Post post)
    {
        post.Title = viewModel.Title;
        post.Content = viewModel.Content;
        post.CategoryId = viewModel.CategoryId;
        post.FeaturedImageAlt = viewModel.FeaturedImageAlt;
        post.IsPublished = viewModel.PublishNow;
        post.Excerpt = string.IsNullOrWhiteSpace(viewModel.Excerpt) ? null : viewModel.Excerpt.Trim();
        post.FeaturedImageUrls = FeaturedImage.Resolve(viewModel.FeaturedImageUrl, post.FeaturedImageUrls);
    }
}
