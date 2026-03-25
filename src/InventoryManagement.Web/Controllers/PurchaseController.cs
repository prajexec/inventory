using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Services;

namespace InventoryManagement.Web.Controllers
{
    [Authorize]
    public class PurchaseController : Controller
    {
        private readonly PurchaseService _purchaseService;
        private readonly SupplierService _supplierService;
        private readonly WarehouseService _warehouseService;
        private readonly ProductService _productService;
        private readonly ActivityLogService _logService;

        public PurchaseController(PurchaseService purchaseService, SupplierService supplierService,
            WarehouseService warehouseService, ProductService productService, ActivityLogService logService)
        {
            _purchaseService = purchaseService;
            _supplierService = supplierService;
            _warehouseService = warehouseService;
            _productService = productService;
            _logService = logService;
        }

        public async Task<IActionResult> Index(OrderStatus? status)
        {
            var orders = await _purchaseService.GetPurchaseOrdersAsync(status);
            ViewBag.Status = status;
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _purchaseService.GetPurchaseOrderByIdAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpGet, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Suppliers = await _supplierService.GetSuppliersAsync();
            ViewBag.Warehouses = await _warehouseService.GetWarehousesAsync();
            ViewBag.Products = (await _productService.GetProductsAsync(pageSize: 1000)).items;
            return View();
        }

        [HttpPost, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Create(int supplierId, int warehouseId, string? notes,
            int[] productIds, int[] quantities, decimal[] unitPrices)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var items = new List<(int, int, decimal)>();
            for (int i = 0; i < productIds.Length; i++)
            {
                if (quantities[i] > 0)
                    items.Add((productIds[i], quantities[i], unitPrices[i]));
            }

            if (!items.Any())
            {
                ViewBag.Error = "Please add at least one item.";
                ViewBag.Suppliers = await _supplierService.GetSuppliersAsync();
                ViewBag.Warehouses = await _warehouseService.GetWarehousesAsync();
                ViewBag.Products = (await _productService.GetProductsAsync(pageSize: 1000)).items;
                return View();
            }

            var (success, orderId, error) = await _purchaseService.CreatePurchaseOrderAsync(
                supplierId, warehouseId, userId, items, notes);

            if (!success)
            {
                ViewBag.Error = error;
                ViewBag.Suppliers = await _supplierService.GetSuppliersAsync();
                ViewBag.Warehouses = await _warehouseService.GetWarehousesAsync();
                ViewBag.Products = (await _productService.GetProductsAsync(pageSize: 1000)).items;
                return View();
            }

            await _logService.LogAsync(userId, "Create", "PurchaseOrder", orderId, "Created purchase order");
            TempData["Success"] = "Purchase order created.";
            return RedirectToAction("Details", new { id = orderId });
        }

        [HttpPost, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var (success, error) = await _purchaseService.UpdateStatusAsync(id, status, userId);

            if (!success)
            {
                TempData["Error"] = error;
                return RedirectToAction("Details", new { id });
            }

            await _logService.LogAsync(userId, "StatusChange", "PurchaseOrder", id, $"Status changed to {status}");
            TempData["Success"] = $"Order status updated to {status}.";
            return RedirectToAction("Details", new { id });
        }
    }
}
