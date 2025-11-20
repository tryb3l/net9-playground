using WebApp.Models;

namespace WebApp.IntegrationTests.Support.Extensions;

public static class DataSeedingExtensions
{
    public static async Task<int> SeedCategoryAsync(this BaseIntegrationTest test, string name)
    {
        return await test.ExecuteDbContextAsync(async db =>
        {
            var category = new Category { Name = name, Slug = name.ToLower().Replace(" ", "-") };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            return category.Id;
        });
    }

    public static async Task<int> SeedPostAsync(this BaseIntegrationTest test, string title, int categoryId)
    {
        return await test.ExecuteDbContextAsync(async db =>
        {
            var post = new Post
            {
                Title = title,
                Slug = title.ToLower().Replace(" ", "-"),
                Content = "Test Content",
                CategoryId = categoryId,
                CreatedAt = DateTime.UtcNow,
                IsPublished = true,
                AuthorId = test.UserContext.CurrentUser?.Id ?? "unknown" 
            };
            db.Posts.Add(post);
            await db.SaveChangesAsync();
            return post.Id;
        });
    }
}