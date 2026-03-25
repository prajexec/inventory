using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Models;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Data;

namespace InventoryManagement.Infrastructure.Services
{
    public class ReportService
    {
        private readonly ApplicationDbContext _db;

        public ReportService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<decimal> GetTotalInventoryValueAsync()
        {
            var inventories = await _db.Inventories
                .Include(i => i.Product)
                .ToListAsync();
            return inventories.Sum(i => (decimal)i.Quantity * i.Product.CostPrice);
        }

        public async Task<List<(Product product, int totalSold)>> GetTopSellingProductsAsync(int count = 10)
        {
            var items = await _db.SalesOrderItems.ToListAsync();
            var grouped = items
                .GroupBy(si => si.ProductId)
                .Select(g => new { ProductId = g.Key, TotalSold = g.Sum(x => x.Quantity - x.ReturnedQuantity) })
                .OrderByDescending(x => x.TotalSold)
                .Take(count)
                .ToList();

            var products = new List<(Product, int)>();
            foreach (var r in grouped)
            {
                var product = await _db.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.Id == r.ProductId);
                if (product != null)
                    products.Add((product, r.TotalSold));
            }
            return products;
        }

        public async Task<List<(string month, decimal revenue)>> GetMonthlyRevenueAsync(int months = 12)
        {
            var startDate = DateTime.UtcNow.AddMonths(-months);

            var orders = await _db.SalesOrders
                .Where(so => so.OrderDate >= startDate && so.Status != SalesOrderStatus.Cancelled)
                .ToListAsync();

            return orders
                .GroupBy(so => so.OrderDate.ToString("yyyy-MM"))
                .Select(g => (month: g.Key, revenue: g.Sum(so => so.TotalAmount)))
                .OrderBy(x => x.month)
                .ToList();
        }

        public async Task<List<(Category category, int productCount, decimal value)>> GetCategoryDistributionAsync()
        {
            var categories = await _db.Categories
                .Include(c => c.Products)
                .ToListAsync();

            var allInventories = await _db.Inventories
                .Include(i => i.Product)
                .ToListAsync();

            var result = new List<(Category, int, decimal)>();
            foreach (var cat in categories)
            {
                var productIds = cat.Products.Select(p => p.Id).ToHashSet();
                var totalValue = allInventories
                    .Where(i => productIds.Contains(i.ProductId))
                    .Sum(i => (decimal)i.Quantity * i.Product.CostPrice);
                result.Add((cat, cat.Products.Count, totalValue));
            }
            return result;
        }

        public async Task<(decimal totalSales, decimal totalPurchases, decimal profit)> GetProfitSummaryAsync(DateTime? from = null, DateTime? to = null)
        {
            var salesQuery = _db.SalesOrders
                .Where(so => so.Status != SalesOrderStatus.Cancelled);
            var purchaseQuery = _db.PurchaseOrders
                .Where(po => po.Status != OrderStatus.Cancelled);

            if (from.HasValue)
            {
                salesQuery = salesQuery.Where(so => so.OrderDate >= from.Value);
                purchaseQuery = purchaseQuery.Where(po => po.OrderDate >= from.Value);
            }
            if (to.HasValue)
            {
                salesQuery = salesQuery.Where(so => so.OrderDate <= to.Value);
                purchaseQuery = purchaseQuery.Where(po => po.OrderDate <= to.Value);
            }

            var salesList = await salesQuery.ToListAsync();
            var purchaseList = await purchaseQuery.ToListAsync();
            var totalSales = salesList.Sum(so => so.TotalAmount);
            var totalPurchases = purchaseList.Sum(po => po.TotalAmount);
            return (totalSales, totalPurchases, totalSales - totalPurchases);
        }

        public async Task<Dictionary<string, int>> GetDashboardStatsAsync()
        {
            return new Dictionary<string, int>
            {
                ["TotalProducts"] = await _db.Products.CountAsync(p => p.IsActive),
                ["TotalCategories"] = await _db.Categories.CountAsync(c => c.IsActive),
                ["TotalSuppliers"] = await _db.Suppliers.CountAsync(s => s.IsActive),
                ["TotalCustomers"] = await _db.Customers.CountAsync(c => c.IsActive),
                ["TotalWarehouses"] = await _db.Warehouses.CountAsync(w => w.IsActive),
                ["PendingPurchaseOrders"] = await _db.PurchaseOrders.CountAsync(po => po.Status == OrderStatus.Pending),
                ["LowStockItems"] = await _db.Inventories.Include(i => i.Product).Where(i => i.Quantity <= i.Product.ReorderLevel).CountAsync(),
                ["TotalSalesOrders"] = await _db.SalesOrders.CountAsync(),
                ["TotalUsers"] = await _db.Users.CountAsync(u => u.IsActive)
            };
        }

        public async Task<List<(string month, decimal sales)>> GetSalesTrendsAsync(int months = 6)
        {
            var startDate = DateTime.UtcNow.AddMonths(-months);

            var orders = await _db.SalesOrders
                .Where(so => so.OrderDate >= startDate && so.Status != SalesOrderStatus.Cancelled)
                .ToListAsync();

            return orders
                .GroupBy(so => so.OrderDate.ToString("MMM yyyy"))
                .Select(g => (month: g.Key, sales: g.Sum(so => so.TotalAmount)))
                .ToList();
        }
    }
}
