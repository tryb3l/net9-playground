using System.Collections.Generic;
using System.Threading.Tasks;
using WebApp.Models;

namespace WebApp.Interfaces
{
    public interface IActivityLogService
    {
        /// <summary>
        /// Records an admin action against the currently signed-in user. No-ops when the
        /// request is unauthenticated, since an activity row requires a user.
        /// </summary>
        Task LogActivityAsync(string actionType, string entityType, string description);

        Task<IEnumerable<ActivityLog>> GetRecentActivitiesAsync(int count = 10);
    }
}
