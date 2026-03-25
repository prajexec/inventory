using System.ComponentModel.DataAnnotations;
using InventoryManagement.Core.Enums;

namespace InventoryManagement.Core.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        [MaxLength(1000)]
        public string? Details { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation
        public User? User { get; set; }
    }

    public class Notification
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Message { get; set; }

        public NotificationType Type { get; set; }

        public bool IsRead { get; set; } = false;

        public int? UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User? User { get; set; }
    }

    public class SystemSetting
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Value { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
