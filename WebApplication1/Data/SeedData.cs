using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WebApplication1.Models;
using WebApplication1.Utils;

namespace WebApplication1.Data;

public class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var root = Directory.GetCurrentDirectory();
        var envFile = Path.Combine(root, ".dev.env");
        DotEnv.Load(envFile);

        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ??
        throw new InvalidOperationException("Admin email not found in environment or configuration.");
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PWORD") ??
        throw new InvalidOperationException("Admin password not found in environment or configuration.");
        var adminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ??
        throw new InvalidOperationException("Admin username not found in environment or configuration.");
        var regularUserName = Environment.GetEnvironmentVariable("USERNAME") ??
        throw new InvalidOperationException("Regular user name not found in environment or configuration.");
        var regularEmail = Environment.GetEnvironmentVariable("EMAIL") ??
        throw new InvalidOperationException("Regular user email not found in environment or configuration.");
        var regularPassword = Environment.GetEnvironmentVariable("PWORD") ??
        throw new InvalidOperationException("Regular user password not found in environment or configuration.");

        try
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                await context.Database.EnsureCreatedAsync();

                try
                {
                    if (context.Users.Any())
                    {
                        Console.WriteLine("Database already seeded with users.");
                        return;
                    }

                    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                    var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

                    if (!await roleManager.RoleExistsAsync("Admin"))
                    {
                        await roleManager.CreateAsync(new IdentityRole("Admin"));
                    }

                    if (!await roleManager.RoleExistsAsync("User"))
                    {
                        await roleManager.CreateAsync(new IdentityRole("User"));
                    }

                    var adminUser = new User
                    {
                        UserName = adminUsername,
                        Email = adminEmail,
                        EmailConfirmed = true,
                        DisplayName = "Admin User"
                    };

                    var result = await userManager.CreateAsync(adminUser, adminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"- {error.Description}");
                        }
                    }

                    var regularUser = new User
                    {
                        UserName = regularUserName,
                        Email = regularEmail,
                        EmailConfirmed = true,
                        DisplayName = "Regular User"
                    };

                    result = await userManager.CreateAsync(regularUser, regularPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(regularUser, "User");

                        var post1 = new Post
                        {
                            Title = "Welcome to My Blog",
                            Content = "This is my first post on this blog platform.",
                            CreatedAt = DateTime.UtcNow,
                            PublishedDate = DateTime.UtcNow,
                            IsPublished = true,
                            AuthorId = regularUser.Id
                        };

                        var post2 = new Post
                        {
                            Title = "Getting Started with .NET 9",
                            Content = "Here are some tips for getting started with the latest version of .NET.",
                            CreatedAt = DateTime.UtcNow,
                            IsPublished = false,
                            AuthorId = regularUser.Id
                        };

                        context.Posts.AddRange(post1, post2);

                        var tag1 = new Tag { Name = "Introduction" };
                        var tag2 = new Tag { Name = ".NET" };
                        var tag3 = new Tag { Name = "Tutorials" };

                        context.Tags.AddRange(tag1, tag2, tag3);
                        await context.SaveChangesAsync();

                        context.PostTags.AddRange(
                            new PostTag { PostId = post1.Id, TagId = tag1.Id },
                            new PostTag { PostId = post2.Id, TagId = tag2.Id },
                            new PostTag { PostId = post2.Id, TagId = tag3.Id }
                        );

                        await context.SaveChangesAsync();
                        Console.WriteLine("Sample blog posts and tags created.");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"- {error.Description}");
                        }
                    }
                }
                catch (PostgresException ex) when (ex.SqlState == "42P01")
                {
                    throw new Exception("Error: Tables don't exist yet. Apply migrations first.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during database seeding: {ex.Message}");

            if (ex.InnerException != null)
            {
                throw new Exception("Inner exception occurred. Check the console for details." + ex.InnerException.Message);
            }
        }
    }
}