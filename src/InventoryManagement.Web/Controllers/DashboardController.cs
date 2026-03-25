using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventoryManagement.Infrastructure.Services;

namespace InventoryManagement.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ReportService _reportService;
        private readonly NotificationService _notificationService;
        private readonly SettingsService _settingsService;

        public DashboardController(ReportService reportService, NotificationService notificationService, SettingsService settingsService)
        {
            _reportService = reportService;
            _notificationService = notificationService;
            _settingsService = settingsService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            ViewBag.Stats = await _reportService.GetDashboardStatsAsync();
            ViewBag.InventoryValue = await _reportService.GetTotalInventoryValueAsync();
            ViewBag.TopSelling = await _reportService.GetTopSellingProductsAsync(5);
            ViewBag.MonthlyRevenue = await _reportService.GetMonthlyRevenueAsync(6);
            ViewBag.CategoryDistribution = await _reportService.GetCategoryDistributionAsync();
            ViewBag.SalesTrends = await _reportService.GetSalesTrendsAsync(6);
            ViewBag.ProfitSummary = await _reportService.GetProfitSummaryAsync();
            ViewBag.CurrencySymbol = await _settingsService.GetCurrencySymbolAsync();

            // Generate alerts
            await _notificationService.GenerateLowStockAlertsAsync();
            await _notificationService.GenerateExpiryAlertsAsync();

            ViewBag.Notifications = await _notificationService.GetNotificationsAsync(userId, true);
            ViewBag.UnreadCount = await _notificationService.GetUnreadCountAsync(userId);

            return View();
        }
    }
}
