using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventoryManagement.Infrastructure.Services;

namespace InventoryManagement.Web.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        private readonly InventoryService _inventoryService;
        private readonly WarehouseService _warehouseService;
        private readonly ProductService _productService;
        private readonly ActivityLogService _logService;

        public InventoryController(InventoryService inventoryService, WarehouseService warehouseService,
            ProductService productService, ActivityLogService logService)
        {
            _inventoryService = inventoryService;
            _warehouseService = warehouseService;
            _productService = productService;
            _logService = logService;
        }

        public async Task<IActionResult> Index(int? warehouseId, string? search)
        {
            var inventory = await _inventoryService.GetInventoryAsync(warehouseId, search);
            ViewBag.Warehouses = await _warehouseService.GetWarehousesAsync();
            ViewBag.WarehouseId = warehouseId;
            ViewBag.Search = search;
            return View(inventory);
        }

        public async Task<IActionResult> LowStock()
        {
            var items = await _inventoryService.GetLowStockItemsAsync();
            return View(items);
        }

        [HttpGet, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Adjust()
        {
            ViewBag.Products = (await _productService.GetProductsAsync(pageSize: 1000)).items;
            ViewBag.Warehouses = await _warehouseService.GetWarehousesAsync();
            return View();
        }

        [HttpPost, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Adjust(int productId, int warehouseId, int quantity, string reason)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var (success, error) = await _inventoryService.AdjustStockAsync(productId, warehouseId, quantity, reason, userId);

            if (!success)
            {
                ViewBag.Error = error;
                ViewBag.Products = (await _productService.GetProductsAsync(pageSize: 1000)).items;
                ViewBag.Warehouses = await _warehouseService.GetWarehousesAsync();
                return View();
            }

            await _logService.LogAsync(userId, "StockAdjust", "Inventory", productId, $"Adjusted by {quantity}: {reason}");
            TempData["Success"] = "Stock adjusted successfully.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Movements(int? productId, int? warehouseId)
        {
            var movements = await _inventoryService.GetStockMovementsAsync(productId, warehouseId);
            ViewBag.ProductId = productId;
            ViewBag.WarehouseId = warehouseId;
            return View(movements);
        }
    }
}
