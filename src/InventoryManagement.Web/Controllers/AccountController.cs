using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using InventoryManagement.Infrastructure.Services;

namespace InventoryManagement.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;
        private readonly ActivityLogService _logService;

        public AccountController(AuthService authService, ActivityLogService logService)
        {
            _authService = authService;
            _logService = logService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Dashboard");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var (user, error) = await _authService.LoginAsync(email, password, ip);

            if (user == null)
            {
                ViewBag.Error = error;
                ViewBag.Email = email;
                return View();
            }

            var principal = _authService.CreateClaimsPrincipal(user);
            await HttpContext.SignInAsync("CookieAuth", principal);

            await _logService.LogAsync(user.Id, "Login", "User", user.Id, "User logged in", ip);
            return RedirectToAction("Index", "Dashboard");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _logService.LogAsync(userId, "Logout", "User", userId, "User logged out");
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }

        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Users()
        {
            var users = await _authService.GetAllUsersAsync();
            ViewBag.Roles = await _authService.GetAllRolesAsync();
            return View(users);
        }

        [HttpGet, Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateUser()
        {
            ViewBag.Roles = await _authService.GetAllRolesAsync();
            return View();
        }

        [HttpPost, Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateUser(string fullName, string email, string password, int[] roleIds)
        {
            var (success, error) = await _authService.CreateUserAsync(fullName, email, password, roleIds);
            if (!success)
            {
                ViewBag.Error = error;
                ViewBag.Roles = await _authService.GetAllRolesAsync();
                return View();
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _logService.LogAsync(userId, "Create", "User", null, $"Created user: {email}");

            TempData["Success"] = "User created successfully.";
            return RedirectToAction("Users");
        }

        [HttpGet, Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _authService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            ViewBag.Roles = await _authService.GetAllRolesAsync();
            return View(user);
        }

        [HttpPost, Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> EditUser(int id, string fullName, string email, bool isActive, int[] roleIds)
        {
            var (success, error) = await _authService.UpdateUserAsync(id, fullName, email, isActive, roleIds);
            if (!success)
            {
                ViewBag.Error = error;
                var user = await _authService.GetUserByIdAsync(id);
                ViewBag.Roles = await _authService.GetAllRolesAsync();
                return View(user);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _logService.LogAsync(userId, "Update", "User", id, $"Updated user: {email}");

            TempData["Success"] = "User updated successfully.";
            return RedirectToAction("Users");
        }

        [HttpGet, Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost, Authorize]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var (success, error) = await _authService.ChangePasswordAsync(userId, currentPassword, newPassword);

            if (!success)
            {
                ViewBag.Error = error;
                return View();
            }

            TempData["Success"] = "Password changed successfully.";
            return RedirectToAction("Index", "Dashboard");
        }

        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> LoginLogs()
        {
            var logs = await _authService.GetLoginLogsAsync();
            return View(logs);
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
