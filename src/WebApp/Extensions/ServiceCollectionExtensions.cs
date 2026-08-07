using WebApp.Interfaces;
using WebApp.Services;

namespace WebApp.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
    }
}
