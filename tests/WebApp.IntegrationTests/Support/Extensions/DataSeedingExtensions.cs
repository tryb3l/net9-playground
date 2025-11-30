using Microsoft.EntityFrameworkCore;
using WebApp.Models;
using WebApp.Utils;

namespace WebApp.IntegrationTests.Support.Extensions;

public static class DataSeedingExtensions
{
    extension(BaseIntegrationTest test)
    {
        public async Task<int> SeedCategoryAsync(string name)
        {
            return await test.ExecuteDbContextAsync(async db =>
            {
                var existing = await db.Categories.FirstOrDefaultAsync(c => c.Name == name);
                if (existing != null) return existing.Id;

                var category = new Category { Name = name, Slug = SlugHelper.GenerateSlug(name) };
                db.Categories.Add(category);
                await db.SaveChangesAsync();
                return category.Id;
            });
        }

        public async Task<(int Id, string Slug)> SeedPostAsync(string title,
            int categoryId,
            bool isPublished = true,
            string? content = null,
            DateTime? createdAt = null)
        {
            return await test.ExecuteDbContextAsync(async db =>
            {
                var authorId = test.UserContext.CurrentUser?.Id;
                if (string.IsNullOrEmpty(authorId))
                {
                    var firstUser = await db.Users.FirstOrDefaultAsync();
                    authorId = firstUser?.Id ?? throw new InvalidOperationException(
                        "Cannot seed Post: No users found in DB. Call GivenAuthenticatedUserAsync() first.");
                }

                var slug = SlugHelper.GenerateSlug(title);
                if (await db.Posts.AnyAsync(p => p.Slug == slug))
                {
                    slug = $"{slug}-{Guid.NewGuid().ToString()[..4]}";
                }

                var post = new Post
                {
                    Title = title,
                    Slug = slug,
                    Content = content ?? "Test Content",
                    CategoryId = categoryId,
                    CreatedAt = createdAt ?? DateTime.UtcNow.AddDays(-2),
                    IsPublished = isPublished,
                    PublishedDate = isPublished ? DateTime.UtcNow : null,
                    AuthorId = authorId
                };

                db.Posts.Add(post);
                await db.SaveChangesAsync();
                return (post.Id, post.Slug);
            });
        }
    }
}