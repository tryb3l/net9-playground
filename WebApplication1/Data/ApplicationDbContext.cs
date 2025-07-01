using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Extensions;
using WebApplication1.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    private readonly IServiceProvider _serviceProvider;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IServiceProvider serviceProvider = null)
        : base(options)
    {
        _serviceProvider = serviceProvider;
    }

    public DbSet<Post> Posts { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<PostTag> PostTags { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }

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

        optionsBuilder
            .UseSeeding((context, seedingData) =>
            {
                var serviceProvider = ((ApplicationDbContext)context)._serviceProvider;
                if (serviceProvider != null)
                    SeedData.SeedSync((ApplicationDbContext)context, serviceProvider);
            })
            .UseAsyncSeeding(async (context, seedingData, cancellationToken) =>
            {
                var serviceProvider = ((ApplicationDbContext)context)._serviceProvider;
                if (serviceProvider != null)
                    await SeedData.SeedAsync((ApplicationDbContext)context, serviceProvider, cancellationToken);
            });
    }
}