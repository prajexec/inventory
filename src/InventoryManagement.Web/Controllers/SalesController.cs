using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventoryManagement.Core.Models;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Services;

namespace InventoryManagement.Web.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly SalesService _salesService;
        private readonly WarehouseService _warehouseService;
        private readonly ProductService _productService;
        private readonly SettingsService _settingsService;
        private readonly ActivityLogService _logService;

        public SalesController(SalesService salesService, WarehouseService warehouseService,
            ProductService productService, SettingsService settingsService, ActivityLogService logService)
        {
            _salesService = salesService;
            _warehouseService = warehouseService;
            _productService = productService;
            _settingsService = settingsService;
            _logService = logService;
        }

        public async Task<IActionResult> Index(SalesOrderStatus? status)
        {
            var orders = await _salesService.GetSalesOrdersAsync(status);
            ViewBag.Status = status;
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _salesService.GetSalesOrderByIdAsync(id);
            if (order == null) return NotFound();
            ViewBag.CurrencySymbol = await _settingsService.GetCurrencySymbolAsync();
            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Customers = await _salesService.GetCustomersAsync();
            ViewBag.Warehouses = await _warehouseService.GetWarehousesAsync();
            ViewBag.Products = (await _productService.GetProductsAsync(pageSize: 1000)).items;
            ViewBag.TaxRate = await _settingsService.GetTaxRateAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(int? customerId, int warehouseId,
            int[] productIds, int[] quantities, decimal[] unitPrices, decimal[] discountPcts,
            decimal orderDiscountPct, decimal taxPct, string? notes)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var items = new List<(int, int, decimal, decimal)>();

            for (int i = 0; i < productIds.Length; i++)
            {
                if (quantities[i] > 0)
                    items.Add((productIds[i], quantities[i], unitPrices[i], discountPcts[i]));
            }

            if (!items.Any())
            {
                ViewBag.Error = "Please add at least one item.";
                ViewBag.Customers = await _salesService.GetCustomersAsync();
                ViewBag.Warehouses = await _warehouseService.GetWarehousesAsync();
                ViewBag.Products = (await _productService.GetProductsAsync(pageSize: 1000)).items;
                ViewBag.TaxRate = await _settingsService.GetTaxRateAsync();
                return View();
            }

            var (success, orderId, error) = await _salesService.CreateSalesOrderAsync(
                customerId, warehouseId, userId, items, orderDiscountPct, taxPct, notes);

            if (!success)
            {
                ViewBag.Error = error;
                ViewBag.Customers = await _salesService.GetCustomersAsync();
                ViewBag.Warehouses = await _warehouseService.GetWarehousesAsync();
                ViewBag.Products = (await _productService.GetProductsAsync(pageSize: 1000)).items;
                ViewBag.TaxRate = await _settingsService.GetTaxRateAsync();
                return View();
            }

            await _logService.LogAsync(userId, "Create", "SalesOrder", orderId, "Created sales order");
            TempData["Success"] = "Sales order created.";
            return RedirectToAction("Details", new { id = orderId });
        }

        [HttpPost, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> ProcessReturn(int salesOrderId, int productId, int returnQuantity)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var (success, error) = await _salesService.ProcessReturnAsync(salesOrderId, productId, returnQuantity, userId);

            if (!success) TempData["Error"] = error;
            else
            {
                await _logService.LogAsync(userId, "Return", "SalesOrder", salesOrderId, $"Returned {returnQuantity} of product #{productId}");
                TempData["Success"] = "Return processed.";
            }
            return RedirectToAction("Details", new { id = salesOrderId });
        }

        // ===== Customers =====
        public async Task<IActionResult> Customers(string? search)
        {
            var customers = await _salesService.GetCustomersAsync(search);
            ViewBag.Search = search;
            return View(customers);
        }

        [HttpGet]
        public IActionResult CreateCustomer() => View();

        [HttpPost]
        public async Task<IActionResult> CreateCustomer(Customer customer)
        {
            var (success, error) = await _salesService.SaveCustomerAsync(customer);
            if (!success) { ViewBag.Error = error; return View(customer); }
            TempData["Success"] = "Customer created.";
            return RedirectToAction("Customers");
        }

        [HttpGet]
        public async Task<IActionResult> EditCustomer(int id)
        {
            var customer = await _salesService.GetCustomerByIdAsync(id);
            if (customer == null) return NotFound();
            return View(customer);
        }

        [HttpPost]
        public async Task<IActionResult> EditCustomer(Customer customer)
        {
            var (success, error) = await _salesService.SaveCustomerAsync(customer);
            if (!success) { ViewBag.Error = error; return View(customer); }
            TempData["Success"] = "Customer updated.";
            return RedirectToAction("Customers");
        }
    }
}
