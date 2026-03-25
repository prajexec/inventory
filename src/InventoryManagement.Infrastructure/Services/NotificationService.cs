using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Models;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Data;

namespace InventoryManagement.Infrastructure.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _db;

        public NotificationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<Notification>> GetNotificationsAsync(int? userId = null, bool unreadOnly = false)
        {
            var query = _db.Notifications.AsQueryable();

            if (userId.HasValue)
                query = query.Where(n => n.UserId == userId.Value || n.UserId == null);

            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            return await query.OrderByDescending(n => n.CreatedAt).Take(50).ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int? userId = null)
        {
            var query = _db.Notifications.Where(n => !n.IsRead);
            if (userId.HasValue)
                query = query.Where(n => n.UserId == userId.Value || n.UserId == null);
            return await query.CountAsync();
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _db.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _db.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(int? userId = null)
        {
            var query = _db.Notifications.Where(n => !n.IsRead);
            if (userId.HasValue)
                query = query.Where(n => n.UserId == userId.Value || n.UserId == null);

            var notifications = await query.ToListAsync();
            foreach (var n in notifications) n.IsRead = true;
            await _db.SaveChangesAsync();
        }

        public async Task CreateNotificationAsync(string title, string message, NotificationType type, int? userId = null)
        {
            _db.Notifications.Add(new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        public async Task GenerateLowStockAlertsAsync()
        {
            var lowStockItems = await _db.Inventories
                .Include(i => i.Product)
                .Include(i => i.Warehouse)
                .Where(i => i.Quantity <= i.Product.ReorderLevel && i.Product.IsActive)
                .ToListAsync();

            foreach (var item in lowStockItems)
            {
                var exists = await _db.Notifications.AnyAsync(n =>
                    n.Type == NotificationType.LowStock &&
                    !n.IsRead &&
                    n.Title.Contains(item.Product.Name));

                if (!exists)
                {
                    await CreateNotificationAsync(
                        $"Low Stock: {item.Product.Name}",
                        $"{item.Product.Name} has only {item.Quantity} units left in {item.Warehouse.Name}. Reorder level: {item.Product.ReorderLevel}",
                        NotificationType.LowStock);
                }
            }
        }

        public async Task GenerateExpiryAlertsAsync(int daysAhead = 30)
        {
            var threshold = DateTime.UtcNow.AddDays(daysAhead);
            var expiringProducts = await _db.Products
                .Where(p => p.ExpiryDate.HasValue && p.ExpiryDate <= threshold && p.IsActive)
                .ToListAsync();

            foreach (var product in expiringProducts)
            {
                var exists = await _db.Notifications.AnyAsync(n =>
                    n.Type == NotificationType.ExpiringProduct &&
                    !n.IsRead &&
                    n.Title.Contains(product.Name));

                if (!exists)
                {
                    await CreateNotificationAsync(
                        $"Expiring: {product.Name}",
                        $"{product.Name} (SKU: {product.SKU}) expires on {product.ExpiryDate:yyyy-MM-dd}",
                        NotificationType.ExpiringProduct);
                }
            }
        }
    }
}
