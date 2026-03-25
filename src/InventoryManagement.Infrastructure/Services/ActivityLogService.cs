using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Models;
using InventoryManagement.Infrastructure.Data;

namespace InventoryManagement.Infrastructure.Services
{
    public class ActivityLogService
    {
        private readonly ApplicationDbContext _db;

        public ActivityLogService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(int? userId, string action, string entityType, int? entityId = null, string? details = null, string? ipAddress = null)
        {
            _db.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        public async Task<List<ActivityLog>> GetLogsAsync(
            int? userId = null, string? entityType = null, int count = 100)
        {
            var query = _db.ActivityLogs
                .Include(a => a.User)
                .AsQueryable();

            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId.Value);
            if (!string.IsNullOrWhiteSpace(entityType))
                query = query.Where(a => a.EntityType == entityType);

            return await query
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync();
        }
    }
}
