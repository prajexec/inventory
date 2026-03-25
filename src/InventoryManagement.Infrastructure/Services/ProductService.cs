using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Models;
using InventoryManagement.Infrastructure.Data;

namespace InventoryManagement.Infrastructure.Services
{
    public class ProductService
    {
        private readonly ApplicationDbContext _db;

        public ProductService(ApplicationDbContext db)
        {
            _db = db;
        }

        // ===== Products =====

        public async Task<(List<Product> items, int total)> GetProductsAsync(
            string? search = null, int? categoryId = null, bool? isActive = null,
            int page = 1, int pageSize = 20)
        {
            var query = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Unit)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(search) ||
                    p.SKU.ToLower().Contains(search) ||
                    (p.Barcode != null && p.Barcode.ToLower().Contains(search)));
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Unit)
                .Include(p => p.ProductSuppliers)
                .ThenInclude(ps => ps.Supplier)
                .Include(p => p.Inventories)
                .ThenInclude(i => i.Warehouse)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<(bool success, string? error)> CreateProductAsync(Product product)
        {
            if (await _db.Products.AnyAsync(p => p.SKU == product.SKU))
                return (false, "A product with this SKU already exists.");

            product.CreatedAt = DateTime.UtcNow;
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool success, string? error)> UpdateProductAsync(Product product)
        {
            var existing = await _db.Products.FindAsync(product.Id);
            if (existing == null) return (false, "Product not found.");

            if (await _db.Products.AnyAsync(p => p.SKU == product.SKU && p.Id != product.Id))
                return (false, "Another product with this SKU already exists.");

            existing.Name = product.Name;
            existing.Description = product.Description;
            existing.SKU = product.SKU;
            existing.Barcode = product.Barcode;
            existing.CostPrice = product.CostPrice;
            existing.SellingPrice = product.SellingPrice;
            existing.ReorderLevel = product.ReorderLevel;
            existing.BatchNumber = product.BatchNumber;
            existing.ExpiryDate = product.ExpiryDate;
            existing.IsActive = product.IsActive;
            existing.CategoryId = product.CategoryId;
            existing.UnitId = product.UnitId;
            existing.UpdatedAt = DateTime.UtcNow;

            if (product.ImagePath != null)
                existing.ImagePath = product.ImagePath;

            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return false;

            product.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        // ===== Categories =====

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _db.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _db.Categories.FindAsync(id);
        }

        public async Task<(bool success, string? error)> SaveCategoryAsync(Category category)
        {
            if (category.Id == 0)
            {
                _db.Categories.Add(category);
            }
            else
            {
                var existing = await _db.Categories.FindAsync(category.Id);
                if (existing == null) return (false, "Category not found.");
                existing.Name = category.Name;
                existing.Description = category.Description;
                existing.IsActive = category.IsActive;
            }
            await _db.SaveChangesAsync();
            return (true, null);
        }

        // ===== Units =====

        public async Task<List<Unit>> GetUnitsAsync()
        {
            return await _db.Units.OrderBy(u => u.Name).ToListAsync();
        }

        public async Task<(bool success, string? error)> SaveUnitAsync(Unit unit)
        {
            if (unit.Id == 0)
            {
                _db.Units.Add(unit);
            }
            else
            {
                var existing = await _db.Units.FindAsync(unit.Id);
                if (existing == null) return (false, "Unit not found.");
                existing.Name = unit.Name;
                existing.Abbreviation = unit.Abbreviation;
            }
            await _db.SaveChangesAsync();
            return (true, null);
        }

        // ===== Product Suppliers =====

        public async Task<(bool success, string? error)> LinkSupplierAsync(int productId, int supplierId, decimal price, bool isPrimary)
        {
            if (await _db.ProductSuppliers.AnyAsync(ps => ps.ProductId == productId && ps.SupplierId == supplierId))
                return (false, "This supplier is already linked to the product.");

            if (isPrimary)
            {
                var existing = await _db.ProductSuppliers
                    .Where(ps => ps.ProductId == productId && ps.IsPrimarySupplier)
                    .ToListAsync();
                foreach (var ps in existing) ps.IsPrimarySupplier = false;
            }

            _db.ProductSuppliers.Add(new ProductSupplier
            {
                ProductId = productId,
                SupplierId = supplierId,
                SupplierPrice = price,
                IsPrimarySupplier = isPrimary
            });
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> UnlinkSupplierAsync(int id)
        {
            var ps = await _db.ProductSuppliers.FindAsync(id);
            if (ps == null) return false;
            _db.ProductSuppliers.Remove(ps);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
