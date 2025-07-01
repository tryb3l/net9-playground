using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Interfaces;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly ApplicationDbContext _context;

        public ActivityLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogActivityAsync(string userId, string actionType, string entityType, string description)
        {
            var log = new ActivityLog
            {
                UserId = userId,
                ActionType = actionType,
                EntityType = entityType,
                Description = description,
                Timestamp = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<ActivityLog>> GetRecentActivitiesAsync(int count = 10)
        {
            return await _context.ActivityLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }
    }
}