using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Models;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Data;

namespace InventoryManagement.Infrastructure.Services
{
    public class SalesService
    {
        private readonly ApplicationDbContext _db;
        private readonly InventoryService _inventoryService;

        public SalesService(ApplicationDbContext db, InventoryService inventoryService)
        {
            _db = db;
            _inventoryService = inventoryService;
        }

        public async Task<List<SalesOrder>> GetSalesOrdersAsync(SalesOrderStatus? status = null)
        {
            var query = _db.SalesOrders
                .Include(so => so.Customer)
                .Include(so => so.Warehouse)
                .Include(so => so.CreatedBy)
                .Include(so => so.Items)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(so => so.Status == status.Value);

            return await query.OrderByDescending(so => so.OrderDate).ToListAsync();
        }

        public async Task<SalesOrder?> GetSalesOrderByIdAsync(int id)
        {
            return await _db.SalesOrders
                .Include(so => so.Customer)
                .Include(so => so.Warehouse)
                .Include(so => so.CreatedBy)
                .Include(so => so.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Unit)
                .Include(so => so.Payments)
                .FirstOrDefaultAsync(so => so.Id == id);
        }

        public async Task<(bool success, int orderId, string? error)> CreateSalesOrderAsync(
            int? customerId, int warehouseId, int userId,
            List<(int productId, int quantity, decimal unitPrice, decimal discountPct)> items,
            decimal orderDiscountPct, decimal taxPct, string? notes = null)
        {
            var orderNumber = $"SO-{DateTime.UtcNow:yyyyMMdd}-{(await _db.SalesOrders.CountAsync() + 1):D4}";

            var so = new SalesOrder
            {
                OrderNumber = orderNumber,
                CustomerId = customerId,
                WarehouseId = warehouseId,
                Status = SalesOrderStatus.Confirmed,
                DiscountPercent = orderDiscountPct,
                TaxPercent = taxPct,
                Notes = notes,
                OrderDate = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            decimal subTotal = 0;
            foreach (var (productId, quantity, unitPrice, discountPct) in items)
            {
                var lineTotal = quantity * unitPrice * (1 - discountPct / 100);
                subTotal += lineTotal;

                so.Items.Add(new SalesOrderItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    DiscountPercent = discountPct,
                    TotalPrice = lineTotal
                });

                // Auto stock deduction
                var (deductOk, deductErr) = await _inventoryService.DeductStockAsync(
                    productId, warehouseId, quantity, orderNumber, userId);

                if (!deductOk)
                    return (false, 0, $"Product #{productId}: {deductErr}");
            }

            so.SubTotal = subTotal;
            so.DiscountAmount = subTotal * (orderDiscountPct / 100);
            var afterDiscount = subTotal - so.DiscountAmount;
            so.TaxAmount = afterDiscount * (taxPct / 100);
            so.TotalAmount = afterDiscount + so.TaxAmount;

            _db.SalesOrders.Add(so);
            await _db.SaveChangesAsync();
            return (true, so.Id, null);
        }

        public async Task<(bool success, string? error)> ProcessReturnAsync(int salesOrderId, int productId, int returnQty, int userId)
        {
            var so = await _db.SalesOrders
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == salesOrderId);

            if (so == null) return (false, "Sales order not found.");

            var item = so.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null) return (false, "Product not found in this order.");

            if (item.ReturnedQuantity + returnQty > item.Quantity)
                return (false, "Return quantity exceeds sold quantity.");

            item.ReturnedQuantity += returnQty;

            // Return stock
            await _inventoryService.AddStockAsync(
                productId, so.WarehouseId, returnQty,
                $"RET-{so.OrderNumber}", userId, MovementType.Return);

            if (so.Items.All(i => i.ReturnedQuantity == i.Quantity))
                so.Status = SalesOrderStatus.Returned;

            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool success, string? error)> RecordPaymentAsync(Payment payment)
        {
            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();
            return (true, null);
        }

        // ===== Customers =====

        public async Task<List<Customer>> GetCustomersAsync(string? search = null)
        {
            var query = _db.Customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(search) ||
                    (c.Email != null && c.Email.ToLower().Contains(search)));
            }

            return await query.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            return await _db.Customers
                .Include(c => c.SalesOrders)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<(bool success, string? error)> SaveCustomerAsync(Customer customer)
        {
            if (customer.Id == 0)
            {
                customer.CreatedAt = DateTime.UtcNow;
                _db.Customers.Add(customer);
            }
            else
            {
                var existing = await _db.Customers.FindAsync(customer.Id);
                if (existing == null) return (false, "Customer not found.");
                existing.Name = customer.Name;
                existing.Email = customer.Email;
                existing.Phone = customer.Phone;
                existing.Address = customer.Address;
                existing.City = customer.City;
                existing.IsActive = customer.IsActive;
            }
            await _db.SaveChangesAsync();
            return (true, null);
        }
    }
}
