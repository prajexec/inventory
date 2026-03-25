using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Models;
using InventoryManagement.Infrastructure.Data;

namespace InventoryManagement.Infrastructure.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _db;
        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 30;

        public AuthService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<(User? user, string? error)> LoginAsync(string email, string password, string? ipAddress)
        {
            var user = await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                await LogLoginAttempt(null, email, false, ipAddress);
                return (null, "Invalid email or password.");
            }

            if (!user.IsActive)
            {
                await LogLoginAttempt(user.Id, email, false, ipAddress);
                return (null, "Account is deactivated. Contact an administrator.");
            }

            if (user.IsLocked && user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            {
                await LogLoginAttempt(user.Id, email, false, ipAddress);
                var remaining = (int)(user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes;
                return (null, $"Account is locked. Try again in {remaining + 1} minutes.");
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.IsLocked = true;
                    user.LockoutEnd = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                }
                await _db.SaveChangesAsync();
                await LogLoginAttempt(user.Id, email, false, ipAddress);

                var attemptsLeft = MaxFailedAttempts - user.FailedLoginAttempts;
                if (attemptsLeft > 0)
                    return (null, $"Invalid password. {attemptsLeft} attempt(s) remaining.");
                return (null, $"Account locked due to too many failed attempts. Try again in {LockoutMinutes} minutes.");
            }

            // Successful login
            user.FailedLoginAttempts = 0;
            user.IsLocked = false;
            user.LockoutEnd = null;
            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await LogLoginAttempt(user.Id, email, true, ipAddress);

            return (user, null);
        }

        public ClaimsPrincipal CreateClaimsPrincipal(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            foreach (var ur in user.UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, ur.Role.Name));
            }

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            return new ClaimsPrincipal(identity);
        }

        public async Task<(bool success, string? error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null) return (false, "User not found.");

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                return (false, "Current password is incorrect.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<string?> GeneratePasswordResetTokenAsync(string email)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return null;

            var token = Guid.NewGuid().ToString("N");
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            await _db.SaveChangesAsync();
            return token;
        }

        public async Task<(bool success, string? error)> ResetPasswordAsync(string token, string newPassword)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.PasswordResetToken == token &&
                u.PasswordResetTokenExpiry > DateTime.UtcNow);

            if (user == null) return (false, "Invalid or expired reset token.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.FailedLoginAttempts = 0;
            user.IsLocked = false;
            user.LockoutEnd = null;
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<(bool success, string? error)> CreateUserAsync(string fullName, string email, string password, int[] roleIds)
        {
            if (await _db.Users.AnyAsync(u => u.Email == email))
                return (false, "A user with this email already exists.");

            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsActive = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            foreach (var roleId in roleIds)
            {
                _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
            }
            await _db.SaveChangesAsync();

            return (true, null);
        }

        public async Task<(bool success, string? error)> UpdateUserAsync(int id, string fullName, string email, bool isActive, int[] roleIds)
        {
            var user = await _db.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return (false, "User not found.");

            if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != id))
                return (false, "Another user with this email already exists.");

            user.FullName = fullName;
            user.Email = email;
            user.IsActive = isActive;

            _db.UserRoles.RemoveRange(user.UserRoles);
            foreach (var roleId in roleIds)
            {
                _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
            }
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            return await _db.Roles.ToListAsync();
        }

        public async Task<List<LoginLog>> GetLoginLogsAsync(int count = 50)
        {
            return await _db.LoginLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        private async Task LogLoginAttempt(int? userId, string email, bool success, string? ip)
        {
            _db.LoginLogs.Add(new LoginLog
            {
                UserId = userId,
                Email = email,
                IsSuccessful = success,
                IpAddress = ip,
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}
