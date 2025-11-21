using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.IntegrationTests.Support.Extensions;

public static class DataSeedingExtensions
{
    public static async Task<int> SeedCategoryAsync(this BaseIntegrationTest test, string name)
    {
        return await test.ExecuteDbContextAsync(async db =>
        {
            var existing = await db.Categories.FirstOrDefaultAsync(c => c.Name == name);
            if (existing != null) return existing.Id;

            var category = new Category { Name = name, Slug = name.ToLower().Replace(" ", "-") };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return category.Id;
        });
    }

    public static async Task<(int Id, string Slug)> SeedPostAsync(this BaseIntegrationTest test, string title, int categoryId)
    {
        return await test.ExecuteDbContextAsync(async db =>
        {
            var authorId = test.UserContext.CurrentUser?.Id;
            if (string.IsNullOrEmpty(authorId))
            {
                var firstUser = await db.Users.FirstOrDefaultAsync();
                authorId = firstUser?.Id ?? throw new InvalidOperationException("Cannot seed Post: No users found in DB. Call GivenAuthenticatedUserAsync() first.");
            }
            
            var slug = title.ToLower().Replace(" ", "-");
            if (await db.Posts.AnyAsync(p => p.Slug == slug))
            {
                slug = $"{slug}-{Guid.NewGuid().ToString()[..4]}";
            }

            var post = new Post
            {
                Title = title,
                Slug = slug,
                Content = "Test Content",
                CategoryId = categoryId,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                IsPublished = true,
                PublishedDate = DateTime.UtcNow.AddDays(-1),
                AuthorId = authorId
            };

            db.Posts.Add(post);
            await db.SaveChangesAsync();
            
            return (post.Id, post.Slug);
        });
    }
}