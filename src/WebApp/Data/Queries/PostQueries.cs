using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Data.Queries;

public static class PostQueries
{
    public static IQueryable<Post> Published(this IQueryable<Post> query) =>
        query.Where(p => p.IsPublished);

    /// <summary>Filters by category <em>slug</em>, so renaming a category keeps links valid.</summary>
    public static IQueryable<Post> InCategory(this IQueryable<Post> query, string? categorySlug) =>
        string.IsNullOrWhiteSpace(categorySlug)
            ? query
            : query.Where(p => p.Category != null && p.Category.Slug == categorySlug);

    /// <summary>Filters by tag <em>slug</em>, so renaming a tag keeps links valid.</summary>
    public static IQueryable<Post> WithTag(this IQueryable<Post> query, string? tagSlug) =>
        string.IsNullOrWhiteSpace(tagSlug)
            ? query
            : query.Where(p => p.PostTags.Any(pt => pt.Tag != null && pt.Tag.Slug == tagSlug));

    public static IQueryable<Post> IncludingDeleted(this IQueryable<Post> query) =>
        query.IgnoreQueryFilters();

    public static IQueryable<Post> WithDetails(this IQueryable<Post> query) =>
        query
            .Include(p => p.Category)
            .Include(p => p.Author)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag);
}
