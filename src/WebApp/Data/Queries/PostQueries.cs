using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Data.Queries;

public static class PostQueries
{
    public static IQueryable<Post> Published(this IQueryable<Post> query) =>
        query.Where(p => p.IsPublished);

    public static IQueryable<Post> InCategory(this IQueryable<Post> query, string? category) =>
        string.IsNullOrWhiteSpace(category)
            ? query
            : query.Where(p => p.Category != null && p.Category.Name == category);

    public static IQueryable<Post> WithTag(this IQueryable<Post> query, string? tag) =>
        string.IsNullOrWhiteSpace(tag)
            ? query
            : query.Where(p => p.PostTags.Any(pt => pt.Tag != null && pt.Tag.Name == tag));

    public static IQueryable<Post> IncludingDeleted(this IQueryable<Post> query) =>
        query.IgnoreQueryFilters();

    public static IQueryable<Post> WithDetails(this IQueryable<Post> query) =>
        query
            .Include(p => p.Category)
            .Include(p => p.Author)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag);
}
