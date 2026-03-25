using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventoryManagement.Infrastructure.Services;

namespace InventoryManagement.Web.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class SettingsController : Controller
    {
        private readonly SettingsService _settingsService;

        public SettingsController(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _settingsService.GetAllSettingsAsync();
            ViewBag.UnreadCount = 0;
            return View(settings);
        }

        [HttpPost]
        public async Task<IActionResult> Update(Dictionary<string, string> settings)
        {
            await _settingsService.UpdateSettingsAsync(settings);
            TempData["Success"] = "Settings updated successfully.";
            return RedirectToAction("Index");
        }
    }
}
