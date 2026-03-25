using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Models;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Data;

namespace InventoryManagement.Infrastructure.Services
{
    public class PurchaseService
    {
        private readonly ApplicationDbContext _db;
        private readonly InventoryService _inventoryService;

        public PurchaseService(ApplicationDbContext db, InventoryService inventoryService)
        {
            _db = db;
            _inventoryService = inventoryService;
        }

        public async Task<List<PurchaseOrder>> GetPurchaseOrdersAsync(OrderStatus? status = null)
        {
            var query = _db.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Include(po => po.CreatedBy)
                .Include(po => po.Items)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(po => po.Status == status.Value);

            return await query.OrderByDescending(po => po.OrderDate).ToListAsync();
        }

        public async Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(int id)
        {
            return await _db.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Warehouse)
                .Include(po => po.CreatedBy)
                .Include(po => po.Items).ThenInclude(i => i.Product)
                .Include(po => po.Invoice)
                .FirstOrDefaultAsync(po => po.Id == id);
        }

        public async Task<(bool success, int orderId, string? error)> CreatePurchaseOrderAsync(
            int supplierId, int warehouseId, int userId,
            List<(int productId, int quantity, decimal unitPrice)> items, string? notes = null)
        {
            var orderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{(await _db.PurchaseOrders.CountAsync() + 1):D4}";

            var po = new PurchaseOrder
            {
                OrderNumber = orderNumber,
                SupplierId = supplierId,
                WarehouseId = warehouseId,
                Status = OrderStatus.Pending,
                Notes = notes,
                OrderDate = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            decimal total = 0;
            foreach (var (productId, quantity, unitPrice) in items)
            {
                var lineTotal = quantity * unitPrice;
                total += lineTotal;
                po.Items.Add(new PurchaseOrderItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = lineTotal
                });
            }
            po.TotalAmount = total;

            _db.PurchaseOrders.Add(po);
            await _db.SaveChangesAsync();
            return (true, po.Id, null);
        }

        public async Task<(bool success, string? error)> UpdateStatusAsync(int orderId, OrderStatus newStatus, int userId)
        {
            var po = await _db.PurchaseOrders
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == orderId);

            if (po == null) return (false, "Purchase order not found.");

            po.Status = newStatus;

            if (newStatus == OrderStatus.Delivered)
            {
                po.DeliveredDate = DateTime.UtcNow;

                // Auto stock update
                foreach (var item in po.Items)
                {
                    item.ReceivedQuantity = item.Quantity;
                    await _inventoryService.AddStockAsync(
                        item.ProductId, po.WarehouseId, item.Quantity,
                        po.OrderNumber, userId, MovementType.StockIn);
                }

                // Generate invoice
                var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{(await _db.PurchaseInvoices.CountAsync() + 1):D4}";
                var invoice = new PurchaseInvoice
                {
                    PurchaseOrderId = po.Id,
                    InvoiceNumber = invoiceNumber,
                    Amount = po.TotalAmount,
                    TaxAmount = po.TotalAmount * 0.18m,
                    TotalAmount = po.TotalAmount * 1.18m,
                    PaymentStatus = PaymentStatus.Pending,
                    InvoiceDate = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(30)
                };
                _db.PurchaseInvoices.Add(invoice);
            }

            await _db.SaveChangesAsync();
            return (true, null);
        }
    }
}
