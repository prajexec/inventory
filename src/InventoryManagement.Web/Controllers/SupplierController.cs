using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventoryManagement.Core.Models;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Services;

namespace InventoryManagement.Web.Controllers
{
    [Authorize]
    public class SupplierController : Controller
    {
        private readonly SupplierService _supplierService;
        private readonly ActivityLogService _logService;

        public SupplierController(SupplierService supplierService, ActivityLogService logService)
        {
            _supplierService = supplierService;
            _logService = logService;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var suppliers = await _supplierService.GetSuppliersAsync(search);
            ViewBag.Search = search;
            return View(suppliers);
        }

        public async Task<IActionResult> Details(int id)
        {
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null) return NotFound();
            ViewBag.OutstandingBalance = await _supplierService.GetOutstandingBalanceAsync(id);
            ViewBag.Payments = await _supplierService.GetPaymentsAsync(id);
            return View(supplier);
        }

        [HttpGet, Authorize(Policy = "ManagerOrAdmin")]
        public IActionResult Create() => View();

        [HttpPost, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            var (success, error) = await _supplierService.SaveSupplierAsync(supplier);
            if (!success) { ViewBag.Error = error; return View(supplier); }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _logService.LogAsync(userId, "Create", "Supplier", supplier.Id, $"Created supplier: {supplier.Name}");
            TempData["Success"] = "Supplier created.";
            return RedirectToAction("Index");
        }

        [HttpGet, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        [HttpPost, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Edit(Supplier supplier)
        {
            var (success, error) = await _supplierService.SaveSupplierAsync(supplier);
            if (!success) { ViewBag.Error = error; return View(supplier); }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _logService.LogAsync(userId, "Update", "Supplier", supplier.Id, $"Updated supplier: {supplier.Name}");
            TempData["Success"] = "Supplier updated.";
            return RedirectToAction("Index");
        }

        [HttpPost, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> RecordPayment(int supplierId, decimal amount, PaymentMethod paymentMethod, string? referenceNumber, int? purchaseOrderId)
        {
            var payment = new SupplierPayment
            {
                SupplierId = supplierId,
                Amount = amount,
                PaymentMethod = paymentMethod,
                ReferenceNumber = referenceNumber,
                PurchaseOrderId = purchaseOrderId
            };
            await _supplierService.RecordPaymentAsync(payment);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _logService.LogAsync(userId, "Payment", "Supplier", supplierId, $"Recorded payment of {amount}");
            TempData["Success"] = "Payment recorded.";
            return RedirectToAction("Details", new { id = supplierId });
        }
    }
}
