using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WebApplication1.Models;
using WebApplication1.Utils;

namespace WebApplication1.Data;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var root = Directory.GetCurrentDirectory();
        var envFile = Path.Combine(root, ".dev.env");
        DotEnv.Load(envFile);

        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ??
                         throw new InvalidOperationException("Admin email not found in environment.");
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PWORD") ??
                            throw new InvalidOperationException("Admin password not found in environment.");
        var adminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ??
                            throw new InvalidOperationException("Admin username not found in environment.");
        var regularUserName = Environment.GetEnvironmentVariable("USERNAME") ??
                              throw new InvalidOperationException("Regular user name not found in environment.");
        var regularEmail = Environment.GetEnvironmentVariable("EMAIL") ??
                           throw new InvalidOperationException("Regular user email not found in environment.");
        var regularPassword = Environment.GetEnvironmentVariable("PWORD") ??
                              throw new InvalidOperationException("Regular user password not found in environment.");

        try
        {
            await using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync();

            await SeedCoreDataAsync(context, serviceProvider, adminEmail, adminUsername, adminPassword, regularEmail,
                regularUserName, regularPassword);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during database seeding: {ex.Message}");
            if (ex.InnerException != null)
            {
                throw new Exception("Inner exception occurred: " + ex.InnerException.Message);
            }

            throw;
        }
    }

    public static void SeedSync(ApplicationDbContext context, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        try
        {
            var root = Directory.GetCurrentDirectory();
            var envFile = Path.Combine(root, ".dev.env");
            DotEnv.Load(envFile);

            var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ??
                             throw new InvalidOperationException("Admin email not found in environment.");
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PWORD") ??
                                throw new InvalidOperationException("Admin password not found in environment.");
            var adminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ??
                                throw new InvalidOperationException("Admin username not found in environment.");
            var regularUserName = Environment.GetEnvironmentVariable("USERNAME") ??
                                  throw new InvalidOperationException("Regular user name not found in environment.");
            var regularEmail = Environment.GetEnvironmentVariable("EMAIL") ??
                               throw new InvalidOperationException("Regular user email not found in environment.");
            var regularPassword = Environment.GetEnvironmentVariable("PWORD") ??
                                  throw new InvalidOperationException(
                                      "Regular user password not found in environment.");

            SeedCoreDataAsync(context, serviceProvider, adminEmail, adminUsername, adminPassword, regularEmail,
                    regularUserName, regularPassword)
                .Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during database seeding: {ex.Message}");
            if (ex.InnerException != null)
            {
                throw new Exception("Inner exception occurred: " + ex.InnerException.Message);
            }

            throw;
        }
    }

    public static async Task SeedAsync(ApplicationDbContext context, IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        try
        {
            var root = Directory.GetCurrentDirectory();
            var envFile = Path.Combine(root, ".dev.env");
            DotEnv.Load(envFile);

            var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ??
                             throw new InvalidOperationException("Admin email not found in environment.");
            var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PWORD") ??
                                throw new InvalidOperationException("Admin password not found in environment.");
            var adminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ??
                                throw new InvalidOperationException("Admin username not found in environment.");
            var regularUserName = Environment.GetEnvironmentVariable("USERNAME") ??
                                  throw new InvalidOperationException("Regular user name not found in environment.");
            var regularEmail = Environment.GetEnvironmentVariable("EMAIL") ??
                               throw new InvalidOperationException("Regular user email not found in environment.");
            var regularPassword = Environment.GetEnvironmentVariable("PWORD") ??
                                  throw new InvalidOperationException(
                                      "Regular user password not found in environment.");

            await SeedCoreDataAsync(context, serviceProvider: serviceProvider, adminEmail, adminUsername, adminPassword,
                regularEmail, regularUserName, regularPassword, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during database seeding: {ex.Message}");
            if (ex.InnerException != null)
            {
                throw new Exception("Inner exception occurred: " + ex.InnerException.Message);
            }

            throw;
        }
    }

    private static async Task SeedCoreDataAsync(ApplicationDbContext context, IServiceProvider serviceProvider,
        string adminEmail, string adminUsername, string adminPassword, string regularEmail, string regularUserName,
        string regularPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine("Starting core data seeding...");
            
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    Console.WriteLine($"Role '{roleName}' created.");
                }
                else
                {
                    Console.WriteLine($"Role '{roleName}' already exists.");
                }
            }

            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
            
            if (!await userManager.Users.AnyAsync(u => u.UserName == adminUsername, cancellationToken))
            {
                var adminUser = new User
                {
                    UserName = adminUsername, Email = adminEmail, EmailConfirmed = true, DisplayName = "Admin User"
                };
                var adminResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (adminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine($"Admin user '{adminUsername}' created and assigned to Admin role.");
                }
                else
                {
                    Console.WriteLine($"Failed to create admin user '{adminUsername}':");
                    foreach (var error in adminResult.Errors) Console.WriteLine($"- {error.Description}");
                }
            }
            else
            {
                Console.WriteLine($"Admin user '{adminUsername}' already exists.");
            }
            
            var regularUser = await userManager.FindByNameAsync(regularUserName);
            if (regularUser == null)
            {
                regularUser = new User
                {
                    UserName = regularUserName,
                    Email = regularEmail,
                    EmailConfirmed = true,
                    DisplayName = "Regular User"
                };
                var regularUserResult = await userManager.CreateAsync(regularUser, regularPassword);
                if (regularUserResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(regularUser, "User");
                    Console.WriteLine($"Regular user '{regularUserName}' created and assigned to User role.");
                }
                else
                {
                    Console.WriteLine($"Failed to create regular user '{regularUserName}':");
                    foreach (var error in regularUserResult.Errors) Console.WriteLine($"- {error.Description}");
                    return;
                }
            }
            else
            {
                Console.WriteLine($"Regular user '{regularUserName}' already exists.");
            }


            var categoriesToUse = new List<Category>();
            if (!await context.Categories.AnyAsync(cancellationToken))
            {
                var sampleCategories = new List<Category>
                {
                    new Category { Name = "Technology", Description = "All about tech.", Slug = "technology" },
                    new Category { Name = "Lifestyle", Description = "Everyday life topics.", Slug = "lifestyle" },
                    new Category { Name = "Travel", Description = "Adventures and explorations.", Slug = "travel" }
                };
                context.Categories.AddRange(sampleCategories);
                try
                {
                    await context.SaveChangesAsync(cancellationToken);
                    Console.WriteLine("Sample categories created and saved.");
                    categoriesToUse.AddRange(sampleCategories);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving categories: {ex.Message}");                                                            
                }
            }
            else
            {
                Console.WriteLine("Categories already exist. Fetching them.");
                categoriesToUse = await context.Categories.ToListAsync(cancellationToken);
            }

            var firstCategory = categoriesToUse.FirstOrDefault();
            if (firstCategory == null)
            {
                Console.WriteLine(
                    "Warning: No categories found or created. Posts will be created without a category if CategoryId is nullable, or may fail if not.");
            }
            
            if (!await context.Posts.AnyAsync(p => p.AuthorId == regularUser.Id,
                    cancellationToken))
            {
                var post1 = new Post
                {
                    Title = "Welcome to My Blog",
                    Content = "This is my first post on this blog platform.",
                    CreatedAt = DateTime.UtcNow,
                    PublishedDate = DateTime.UtcNow,
                    IsPublished = true,
                    AuthorId = regularUser.Id,
                    CategoryId = firstCategory?.Id
                };

                var post2 = new Post
                {
                    Title = "Getting Started with .NET 9",
                    Content = "Here are some tips for getting started with the latest version of .NET.",
                    CreatedAt = DateTime.UtcNow,
                    IsPublished = false,
                    AuthorId = regularUser.Id,
                    CategoryId = firstCategory?.Id
                };
                context.Posts.AddRange(post1, post2);
                Console.WriteLine("Sample posts prepared.");
            }
            else
            {
                Console.WriteLine($"User '{regularUserName}' already has posts. Skipping post seeding for this user.");
            }
            
            var tagsToUse = new List<Tag>();
            if (!await context.Tags.AnyAsync(cancellationToken))
            {
                var sampleTags = new List<Tag>
                {
                    new Tag { Name = "Introduction" }, new Tag { Name = ".NET" }, new Tag { Name = "Tutorials" }
                };
                context.Tags.AddRange(sampleTags);
                Console.WriteLine("Sample tags prepared.");
                tagsToUse.AddRange(sampleTags);
            }
            else
            {
                Console.WriteLine("Tags already exist. Fetching them for PostTag associations.");
                tagsToUse = await context.Tags.ToListAsync(cancellationToken);
            }
            
            try
            {
                await context.SaveChangesAsync(cancellationToken);
                Console.WriteLine("Posts and Tags (and potentially Categories) saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving posts/tags: {ex.Message}");
            }
                      
            var welcomePost =
                await context.Posts.FirstOrDefaultAsync(
                    p => p.Title == "Welcome to My Blog" && p.AuthorId == regularUser.Id, cancellationToken);
            var dotnetPost = await context.Posts.FirstOrDefaultAsync(
                p => p.Title == "Getting Started with .NET 9" && p.AuthorId == regularUser.Id, cancellationToken);
            var introTag = tagsToUse.FirstOrDefault(t => t.Name == "Introduction") ??
                           await context.Tags.FirstOrDefaultAsync(t => t.Name == "Introduction", cancellationToken);
            var dotnetTag = tagsToUse.FirstOrDefault(t => t.Name == ".NET") ??
                            await context.Tags.FirstOrDefaultAsync(t => t.Name == ".NET", cancellationToken);
            var tutorialsTag = tagsToUse.FirstOrDefault(t => t.Name == "Tutorials") ??
                               await context.Tags.FirstOrDefaultAsync(t => t.Name == "Tutorials", cancellationToken);

            if (welcomePost != null && introTag != null &&
                !await context.PostTags.AnyAsync(pt => pt.PostId == welcomePost.Id && pt.TagId == introTag.Id,
                    cancellationToken))
            {
                context.PostTags.Add(new PostTag { PostId = welcomePost.Id, TagId = introTag.Id });
            }

            if (dotnetPost != null && dotnetTag != null &&
                !await context.PostTags.AnyAsync(pt => pt.PostId == dotnetPost.Id && pt.TagId == dotnetTag.Id,
                    cancellationToken))
            {
                context.PostTags.Add(new PostTag { PostId = dotnetPost.Id, TagId = dotnetTag.Id });
            }

            if (dotnetPost != null && tutorialsTag != null &&
                !await context.PostTags.AnyAsync(pt => pt.PostId == dotnetPost.Id && pt.TagId == tutorialsTag.Id,
                    cancellationToken))
            {
                context.PostTags.Add(new PostTag { PostId = dotnetPost.Id, TagId = tutorialsTag.Id });
            }

            if (context.ChangeTracker.HasChanges())
            {
                try
                {
                    await context.SaveChangesAsync(cancellationToken);
                    Console.WriteLine("Post-Tag associations created/updated successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving PostTags: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("No new Post-Tag associations to create.");
            }

            Console.WriteLine("Core data seeding completed.");
        }
        catch (PostgresException pgEx) when (pgEx.SqlState == "42P01")
        {
            Console.WriteLine(
                $"SEEDING ERROR: Tables don't exist yet. Apply migrations first. Details: {pgEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred during database seeding: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            
            throw;
        }
    }
}