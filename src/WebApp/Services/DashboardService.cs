using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApp.Areas.Admin.ViewModels.Dashboard;
using WebApp.Data;
using WebApp.Interfaces;
using WebApp.Mapping;
using WebApp.Models;

namespace WebApp.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly IActivityLogService _activityLogService;

    public DashboardService(
        ApplicationDbContext db,
        IActivityLogService activityLogService,
        UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
        _activityLogService = activityLogService;
    }

    public async Task<DashboardViewModel> GetDashboardViewModelAsync()
    {
        var totalPosts = await _db.Posts.CountAsync();
        var totalCategories = await _db.Categories.CountAsync();
        var totalTags = await _db.Tags.CountAsync();
        var totalUsers = await _userManager.Users.CountAsync();
        var recentActivities = await _activityLogService.GetRecentActivitiesAsync();

        var recentPosts = await _db.Posts
            .AsNoTracking()
            .Include(p => p.Author)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .ToListAsync();

        var recentPostViewModels = recentPosts.Select(p => p.ToRecentPostViewModel()).ToList();

        return new DashboardViewModel
        {
            TotalPosts = totalPosts,
            TotalCategories = totalCategories,
            TotalTags = totalTags,
            TotalUsers = totalUsers,
            RecentActivities = recentActivities,
            RecentPosts = recentPostViewModels
        };
    }
}