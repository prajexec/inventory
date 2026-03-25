using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InventoryManagement.Core.Models;
using InventoryManagement.Infrastructure.Services;

namespace InventoryManagement.Web.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly ProductService _productService;
        private readonly ActivityLogService _logService;

        public ProductController(ProductService productService, ActivityLogService logService)
        {
            _productService = productService;
            _logService = logService;
        }

        public async Task<IActionResult> Index(string? search, int? categoryId, int page = 1)
        {
            var (items, total) = await _productService.GetProductsAsync(search, categoryId, null, page);
            ViewBag.Categories = await _productService.GetCategoriesAsync();
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / 20.0);
            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpGet, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _productService.GetCategoriesAsync();
            ViewBag.Units = await _productService.GetUnitsAsync();
            return View();
        }

        [HttpPost, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            if (imageFile != null)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                var path = Path.Combine("wwwroot/uploads", fileName);
                Directory.CreateDirectory("wwwroot/uploads");
                using var stream = new FileStream(path, FileMode.Create);
                await imageFile.CopyToAsync(stream);
                product.ImagePath = $"/uploads/{fileName}";
            }

            var (success, error) = await _productService.CreateProductAsync(product);
            if (!success)
            {
                ViewBag.Error = error;
                ViewBag.Categories = await _productService.GetCategoriesAsync();
                ViewBag.Units = await _productService.GetUnitsAsync();
                return View(product);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _logService.LogAsync(userId, "Create", "Product", product.Id, $"Created product: {product.Name}");

            TempData["Success"] = "Product created successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            ViewBag.Categories = await _productService.GetCategoriesAsync();
            ViewBag.Units = await _productService.GetUnitsAsync();
            return View(product);
        }

        [HttpPost, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            if (imageFile != null)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
                var path = Path.Combine("wwwroot/uploads", fileName);
                Directory.CreateDirectory("wwwroot/uploads");
                using var stream = new FileStream(path, FileMode.Create);
                await imageFile.CopyToAsync(stream);
                product.ImagePath = $"/uploads/{fileName}";
            }

            var (success, error) = await _productService.UpdateProductAsync(product);
            if (!success)
            {
                ViewBag.Error = error;
                ViewBag.Categories = await _productService.GetCategoriesAsync();
                ViewBag.Units = await _productService.GetUnitsAsync();
                return View(product);
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _logService.LogAsync(userId, "Update", "Product", product.Id, $"Updated product: {product.Name}");

            TempData["Success"] = "Product updated successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteProductAsync(id);
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _logService.LogAsync(userId, "Delete", "Product", id, "Deactivated product");

            TempData["Success"] = "Product deactivated.";
            return RedirectToAction("Index");
        }

        // ===== Categories =====
        public async Task<IActionResult> Categories()
        {
            var categories = await _productService.GetCategoriesAsync();
            return View(categories);
        }

        [HttpPost, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> SaveCategory(Category category)
        {
            await _productService.SaveCategoryAsync(category);
            TempData["Success"] = "Category saved.";
            return RedirectToAction("Categories");
        }

        // ===== Units =====
        public async Task<IActionResult> Units()
        {
            var units = await _productService.GetUnitsAsync();
            return View(units);
        }

        [HttpPost, Authorize(Policy = "ManagerOrAdmin")]
        public async Task<IActionResult> SaveUnit(Unit unit)
        {
            await _productService.SaveUnitAsync(unit);
            TempData["Success"] = "Unit saved.";
            return RedirectToAction("Units");
        }
    }
}
