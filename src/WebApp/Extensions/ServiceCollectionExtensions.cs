using WebApp.Interfaces;
using WebApp.Repositories;
using WebApp.Services;

namespace WebApp.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Repository registrations
        services.AddScoped<IPostRepository, PostRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        // Service registrations
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<IAttachmentService, AttachmentService>();

        return services;
    }
}
