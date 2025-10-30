using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.Extensions;

namespace WebApp.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    private readonly IServiceProvider? _serviceProvider;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IServiceProvider? serviceProvider = null)
        : base(options)
    {
        _serviceProvider = serviceProvider;
    }

    public DbSet<Post> Posts { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Tag> Tags { get; set; } = null!;
    public DbSet<PostTag> PostTags { get; set; } = null!;
    public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PostTag>()
            .HasKey(pt => new { pt.PostId, pt.TagId });

        modelBuilder.Entity<PostTag>()
            .HasOne(pt => pt.Post)
            .WithMany(p => p.PostTags)
            .HasForeignKey(pt => pt.PostId);

        modelBuilder.Entity<PostTag>()
            .HasOne(pt => pt.Tag)
            .WithMany(t => t.PostTags)
            .HasForeignKey(pt => pt.TagId);

        modelBuilder.Entity<Post>()
            .HasOne(p => p.Author)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.AuthorId);

        modelBuilder.Entity<Post>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Posts)
            .HasForeignKey(p => p.CategoryId);

        modelBuilder.SetQueryFilterOnAllEntities<ISoftDelete>(e => !e.IsDeleted);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Skip EF seeding when running integration tests
        var disableSeeding = Environment.GetEnvironmentVariable("DISABLE_DB_SEEDING");
        if (_serviceProvider != null &&
            !string.Equals(disableSeeding, "true", StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder
                .UseSeeding((context, seedingData) =>
                {
                    var serviceProvider = ((ApplicationDbContext)context)._serviceProvider;
                    SeedData.SeedSync((ApplicationDbContext)context, serviceProvider);
                })
                .UseAsyncSeeding(async (context, seedingData, cancellationToken) =>
                {
                    var serviceProvider = ((ApplicationDbContext)context)._serviceProvider;
                    await SeedData.SeedAsync((ApplicationDbContext)context, serviceProvider, cancellationToken);
                });
        }
    }
}