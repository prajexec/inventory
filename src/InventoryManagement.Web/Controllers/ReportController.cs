using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventoryManagement.Infrastructure.Services;

namespace InventoryManagement.Web.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ReportService _reportService;
        private readonly InventoryService _inventoryService;
        private readonly SettingsService _settingsService;

        public ReportController(ReportService reportService, InventoryService inventoryService, SettingsService settingsService)
        {
            _reportService = reportService;
            _inventoryService = inventoryService;
            _settingsService = settingsService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.InventoryValue = await _reportService.GetTotalInventoryValueAsync();
            ViewBag.TopSelling = await _reportService.GetTopSellingProductsAsync(10);
            ViewBag.MonthlyRevenue = await _reportService.GetMonthlyRevenueAsync(12);
            ViewBag.CategoryDistribution = await _reportService.GetCategoryDistributionAsync();
            ViewBag.ProfitSummary = await _reportService.GetProfitSummaryAsync();
            ViewBag.LowStock = await _inventoryService.GetLowStockItemsAsync();
            ViewBag.CurrencySymbol = await _settingsService.GetCurrencySymbolAsync();
            return View();
        }
    }
}
