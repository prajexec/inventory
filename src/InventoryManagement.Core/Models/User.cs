using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Core.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public bool IsLocked { get; set; } = false;
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }

        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpiry { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // Navigation
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public ICollection<LoginLog> LoginLogs { get; set; } = new List<LoginLog>();
    }

    public class Role
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Navigation
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    public class UserRole
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }

        // Navigation
        public User User { get; set; } = null!;
        public Role Role { get; set; } = null!;
    }

    public class LoginLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }

        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        public bool IsSuccessful { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation
        public User? User { get; set; }
    }
}
