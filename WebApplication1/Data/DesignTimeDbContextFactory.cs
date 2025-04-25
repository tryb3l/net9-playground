using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WebApplication1.Utils;

namespace WebApplication1.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var root = Directory.GetCurrentDirectory();
        Console.WriteLine($"Current directory: {root}");

        // Try multiple possible locations for the .dev.env file
        var possiblePaths = new[] {
            Path.Combine(root, ".dev.env"),
            Path.Combine(root, "WebApplication1", ".dev.env"),
            Path.Combine(root, "..", ".dev.env"),
            Path.Combine(root, "WebApplication1/.dev.env")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                Console.WriteLine($"Found .dev.env at: {path}");
                DotEnv.Load(path);
                break;
            }
            else
            {
                Console.WriteLine($"No .dev.env at: {path}");
            }
        }

        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("CONNECTION_STRING not found in environment.");
            Console.WriteLine("Please set CONNECTION_STRING environment variable before running migrations.");
            Console.WriteLine("Or use: make add-migration name=AddSoftDelete");

            throw new InvalidOperationException("CONNECTION_STRING environment variable is required");
        }

        Console.WriteLine("Using CONNECTION_STRING from environment");
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}