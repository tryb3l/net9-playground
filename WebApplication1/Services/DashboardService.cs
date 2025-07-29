using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Areas.Admin.ViewModels.Dashboard;
using WebApplication1.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Services;

public class DashboardService : IDashboardService
{
    private readonly IPostRepository _postRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ITagRepository _tagRepository;
    private readonly UserManager<User> _userManager;
    private readonly IActivityLogService _activityLogService;
    private readonly IMapper _mapper;

    public DashboardService(
        IPostRepository postRepository,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        IActivityLogService activityLogService,
        UserManager<User> userManager,
        IMapper mapper)
    {
        _postRepository = postRepository;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
        _userManager = userManager;
        _activityLogService = activityLogService;
        _mapper = mapper;
    }

    public async Task<DashboardViewModel> GetDashboardViewModelAsync()
    {
        var totalPosts = await _postRepository.CountAllAsync(p => !p.IsDeleted);
        var totalCategories = await _categoryRepository.CountAllAsync();
        var totalTags = await _tagRepository.CountAllAsync();
        var totalUsers = await _userManager.Users.CountAsync();
        var recentActivities = await _activityLogService.GetRecentActivitiesAsync();
        
        var recentPosts = await _postRepository.GetRecentPostsAsync(5);

        var recentPostViewModels = _mapper.Map<List<RecentPostViewModel>>(recentPosts);

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