using System.Collections.Generic;
using System.Threading.Tasks;
using WebApp.Models;

namespace WebApp.Interfaces
{
    public interface IActivityLogService
    {
        Task LogActivityAsync(string userId, string actionType, string entityType, string description);
        Task<IEnumerable<ActivityLog>> GetRecentActivitiesAsync(int count = 10);
    }
}