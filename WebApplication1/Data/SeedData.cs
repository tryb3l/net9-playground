using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WebApplication1.Models;

namespace WebApplication1.Data;

public class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
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

                    Console.WriteLine("Seeding database...");

                    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                    var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

                    if (!await roleManager.RoleExistsAsync("Admin"))
                    {
                        await roleManager.CreateAsync(new IdentityRole("Admin"));
                        Console.WriteLine("Admin role created.");
                    }

                    if (!await roleManager.RoleExistsAsync("User"))
                    {
                        await roleManager.CreateAsync(new IdentityRole("User"));
                        Console.WriteLine("User role created.");
                    }

                    var adminUser = new User
                    {
                        UserName = "admin@example.com",
                        Email = "admin@example.com",
                        EmailConfirmed = true,
                        DisplayName = "Admin User"
                    };

                    var result = await userManager.CreateAsync(adminUser, "AdminPassword123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                        Console.WriteLine("Admin user created and assigned to Admin role.");
                    }
                    else
                    {
                        Console.WriteLine("Failed to create admin user:");
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"- {error.Description}");
                        }
                    }

                    var regularUser = new User
                    {
                        UserName = "user@example.com",
                        Email = "user@example.com",
                        EmailConfirmed = true,
                        DisplayName = "Regular User"
                    };

                    result = await userManager.CreateAsync(regularUser, "UserPassword123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(regularUser, "User");
                        Console.WriteLine("Regular user created and assigned to User role.");

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
                        Console.WriteLine("Failed to create regular user:");
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"- {error.Description}");
                        }
                    }
                }
                catch (PostgresException ex) when (ex.SqlState == "42P01")
                {
                    Console.WriteLine("Error: Tables don't exist yet. Apply migrations first.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred during database seeding: {ex.Message}");

            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }
}