using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventoryManagement.Infrastructure.Services;

namespace InventoryManagement.Web.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class ActivityLogController : Controller
    {
        private readonly ActivityLogService _activityLogService;

        public ActivityLogController(ActivityLogService activityLogService)
        {
            _activityLogService = activityLogService;
        }

        public async Task<IActionResult> Index()
        {
            var logs = await _activityLogService.GetLogsAsync(count: 200);
            ViewBag.UnreadCount = 0;
            return View(logs);
        }
    }
}
