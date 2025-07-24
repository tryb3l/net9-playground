using System.Collections.Generic;
using WebApplication1.Models;

namespace WebApplication1.Areas.Admin.ViewModels.Dashboard
{
    public class RecentPostViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
    }

    public class DashboardViewModel
    {
        public int TotalPosts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalTags { get; set; }
        public int TotalUsers { get; set; }
        public IEnumerable<ActivityLog> RecentActivities { get; init; } = new List<ActivityLog>();
        public IEnumerable<RecentPostViewModel> RecentPosts { get; set; } = new List<RecentPostViewModel>();
    }
}