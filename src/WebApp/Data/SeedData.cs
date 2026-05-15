using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Data;

public static class SeedData
{
    public static async Task SeedAsync(ApplicationDbContext context, IServiceProvider? serviceProvider, CancellationToken cancellationToken = default)
    {
        if (serviceProvider == null)
        {
            Console.WriteLine("ServiceProvider is null, skipping seeding");
            return;
        }

        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PWORD");
        var adminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME");

        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword) || string.IsNullOrEmpty(adminUsername))
        {
            Console.WriteLine("Admin credentials not found in environment, skipping seeding");
            return;
        }

        await SeedCoreDataAsync(context, serviceProvider, adminEmail, adminUsername, adminPassword, cancellationToken);
    }

    public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ??
                         throw new InvalidOperationException("Admin email not found in environment.");
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PWORD") ??
                            throw new InvalidOperationException("Admin password not found in environment.");
        var adminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ??
                            throw new InvalidOperationException("Admin username not found in environment.");

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await SeedCoreDataAsync(context, scope.ServiceProvider, adminEmail, adminUsername, adminPassword, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during database seeding: {ex.Message}");
            throw;
        }
    }

    private static async Task SeedCoreDataAsync(
        ApplicationDbContext context,
        IServiceProvider serviceProvider,
        string adminEmail,
        string adminUsername,
        string adminPassword,
        CancellationToken cancellationToken = default)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Seed roles
        string[] roles = ["Admin", "User"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed admin user
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminUsername,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = "Administrator"
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine($"Admin user '{adminEmail}' created successfully");
            }
            else
            {
                Console.WriteLine($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        // Seed default categories
        if (!await context.Categories.AnyAsync(cancellationToken))
        {
            context.Categories.AddRange(
                new Category { Name = "General", Slug = "general", Description = "General posts" },
                new Category { Name = "Technology", Slug = "technology", Description = "Tech-related posts" },
                new Category { Name = "Lifestyle", Slug = "lifestyle", Description = "Lifestyle posts" }
            );
            await context.SaveChangesAsync(cancellationToken);
        }

        // Seed default tags
        if (!await context.Tags.AnyAsync(cancellationToken))
        {
            context.Tags.AddRange(
                new Tag { Name = "Tutorial" },
                new Tag { Name = "News" },
                new Tag { Name = "Review" }
            );
            await context.SaveChangesAsync(cancellationToken);
        }

        // Seed posts
        if (!await context.Posts.AnyAsync(cancellationToken))
        {
            var techCategory = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "technology", cancellationToken);
            var tutorialTag = await context.Tags.FirstOrDefaultAsync(t => t.Name == "Tutorial", cancellationToken);

            if (techCategory != null)
            {
                var posts = new List<Post>
                {
                    new()
                    {
                        Title = "Welcome to the Blog",
                        Slug = "welcome-to-the-blog",
                        Content = "This is a sample post to demonstrate the blog functionality. You can edit or delete it.",
                        AuthorId = adminUser.Id,
                        CategoryId = techCategory.Id,
                        IsPublished = true,
                        CreatedAt = DateTime.UtcNow,
                        PublishedDate = DateTime.UtcNow,
                        FeaturedImageUrls = "{\"large\":\"https://placehold.co/1920x1080\",\"medium\":\"https://placehold.co/800x450\",\"thumbnail\":\"https://placehold.co/400x225\"}"
                    },
                    new()
                    {
                        Title = "Draft Post Example",
                        Slug = "draft-post-example",
                        Content = "This is a draft post. It is not visible to the public.",
                        AuthorId = adminUser.Id,
                        CategoryId = techCategory.Id,
                        IsPublished = false,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                context.Posts.AddRange(posts);
                await context.SaveChangesAsync(cancellationToken);

                if (tutorialTag != null)
                {
                    foreach (var post in posts)
                    {
                        context.PostTags.Add(new PostTag { PostId = post.Id, TagId = tutorialTag.Id });
                    }
                    await context.SaveChangesAsync(cancellationToken);
                }
                Console.WriteLine("Seeded default posts");
            }
        }
    }
}