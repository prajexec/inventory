using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventoryManagement.Core.Models;
using InventoryManagement.Infrastructure.Services;

namespace InventoryManagement.Web.Controllers
{
    [Authorize]
    public class WarehouseController : Controller
    {
        private readonly WarehouseService _warehouseService;
        private readonly ProductService _productService;
        private readonly AuthService _authService;
        private readonly ActivityLogService _logService;

        public WarehouseController(WarehouseService warehouseService, ProductService productService,
            AuthService authService, ActivityLogService logService)
        {
            _warehouseService = warehouseService;
            _productService = productService;
            _authService = authService;
            _logService = logService;
        }

        public async Task<IActionResult> Index()
        {
            var warehouses = await _warehouseService.GetWarehousesAsync();
            return View(warehouses);
        }

        public async Task<IActionResult> Details(int id)
        {
            var warehouse = await _warehouseService.GetWarehouseByIdAsync(id);
            if (warehouse == null) return NotFound();
            return View(warehouse);
        }

        [HttpGet, Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Users = await _authService.GetAllUsersAsync();
            return View();
        }

        [HttpPost, Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(Warehouse warehouse)
        {
            var (success, error) = await _warehouseService.SaveWarehouseAsync(warehouse);
            if (!success) { ViewBag.Error = error; ViewBag.Users = await _authService.GetAllUsersAsync(); return View(warehouse); }
            TempData["Success"] = "Warehouse created.";
            return RedirectToAction("Index");
        }

        [HttpGet, Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(int id)
        {
            var warehouse = await _warehouseService.GetWarehouseByIdAsync(id);
            if (warehouse == null) return NotFound();
            ViewBag.Users = await _authService.GetAllUsersAsync();
            return View(warehouse);
        }

        [HttpPost, Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Edit(Warehouse warehouse)
        {
            var (success, error) = await _warehouseService.SaveWarehouseAsync(warehouse);
            if (!success) { ViewBag.Error = error; ViewBag.Users = await _authService.GetAllUsersAsync(); return View(warehouse); }
            TempData["Success"] = "Warehouse updated.";
            return RedirectToAction("Index");
        }

        [HttpGet, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Transfer()
        {
            ViewBag.Warehouses = await _warehouseService.GetWarehousesAsync();
            ViewBag.Products = (await _productService.GetProductsAsync(pageSize: 1000)).items;
            return View();
        }

        [HttpPost, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Transfer(int fromWarehouseId, int toWarehouseId,
            int[] productIds, int[] quantities, string? notes)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var items = new List<(int, int)>();
            for (int i = 0; i < productIds.Length; i++)
            {
                if (quantities[i] > 0) items.Add((productIds[i], quantities[i]));
            }

            if (!items.Any())
            {
                ViewBag.Error = "Please add at least one item.";
                ViewBag.Warehouses = await _warehouseService.GetWarehousesAsync();
                ViewBag.Products = (await _productService.GetProductsAsync(pageSize: 1000)).items;
                return View();
            }

            var (success, transferId, error) = await _warehouseService.CreateTransferAsync(
                fromWarehouseId, toWarehouseId, userId, items, notes);

            if (!success) { TempData["Error"] = error; return RedirectToAction("Transfers"); }

            // Auto-complete
            var (completeOk, completeErr) = await _warehouseService.CompleteTransferAsync(transferId, userId);
            if (!completeOk) { TempData["Error"] = completeErr; }
            else { TempData["Success"] = "Transfer completed."; }

            return RedirectToAction("Transfers");
        }

        public async Task<IActionResult> Transfers()
        {
            var transfers = await _warehouseService.GetTransfersAsync();
            return View(transfers);
        }
    }
}
