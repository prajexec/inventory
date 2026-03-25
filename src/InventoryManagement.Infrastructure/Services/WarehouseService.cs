using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Models;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Data;

namespace InventoryManagement.Infrastructure.Services
{
    public class WarehouseService
    {
        private readonly ApplicationDbContext _db;
        private readonly InventoryService _inventoryService;

        public WarehouseService(ApplicationDbContext db, InventoryService inventoryService)
        {
            _db = db;
            _inventoryService = inventoryService;
        }

        public async Task<List<Warehouse>> GetWarehousesAsync()
        {
            return await _db.Warehouses
                .Include(w => w.Manager)
                .OrderBy(w => w.Name)
                .ToListAsync();
        }

        public async Task<Warehouse?> GetWarehouseByIdAsync(int id)
        {
            return await _db.Warehouses
                .Include(w => w.Manager)
                .Include(w => w.Inventories).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(w => w.Id == id);
        }

        public async Task<(bool success, string? error)> SaveWarehouseAsync(Warehouse warehouse)
        {
            if (warehouse.Id == 0)
            {
                _db.Warehouses.Add(warehouse);
            }
            else
            {
                var existing = await _db.Warehouses.FindAsync(warehouse.Id);
                if (existing == null) return (false, "Warehouse not found.");
                existing.Name = warehouse.Name;
                existing.Address = warehouse.Address;
                existing.City = warehouse.City;
                existing.Phone = warehouse.Phone;
                existing.IsActive = warehouse.IsActive;
                existing.ManagerUserId = warehouse.ManagerUserId;
            }
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool success, int transferId, string? error)> CreateTransferAsync(
            int fromWarehouseId, int toWarehouseId, int userId,
            List<(int productId, int quantity)> items, string? notes = null)
        {
            var transferNumber = $"TRF-{DateTime.UtcNow:yyyyMMddHHmmss}";

            var transfer = new WarehouseTransfer
            {
                TransferNumber = transferNumber,
                FromWarehouseId = fromWarehouseId,
                ToWarehouseId = toWarehouseId,
                Status = TransferStatus.Pending,
                Notes = notes,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var (productId, quantity) in items)
            {
                transfer.TransferItems.Add(new TransferItem
                {
                    ProductId = productId,
                    Quantity = quantity
                });
            }

            _db.WarehouseTransfers.Add(transfer);
            await _db.SaveChangesAsync();
            return (true, transfer.Id, null);
        }

        public async Task<(bool success, string? error)> CompleteTransferAsync(int transferId, int userId)
        {
            var transfer = await _db.WarehouseTransfers
                .Include(t => t.TransferItems)
                .FirstOrDefaultAsync(t => t.Id == transferId);

            if (transfer == null) return (false, "Transfer not found.");
            if (transfer.Status == TransferStatus.Completed) return (false, "Transfer already completed.");

            foreach (var item in transfer.TransferItems)
            {
                var (deductOk, deductErr) = await _inventoryService.DeductStockAsync(
                    item.ProductId, transfer.FromWarehouseId, item.Quantity,
                    transfer.TransferNumber, userId);

                if (!deductOk) return (false, $"Product #{item.ProductId}: {deductErr}");

                await _inventoryService.AddStockAsync(
                    item.ProductId, transfer.ToWarehouseId, item.Quantity,
                    transfer.TransferNumber, userId, MovementType.Transfer);
            }

            transfer.Status = TransferStatus.Completed;
            transfer.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<WarehouseTransfer>> GetTransfersAsync()
        {
            return await _db.WarehouseTransfers
                .Include(t => t.FromWarehouse)
                .Include(t => t.ToWarehouse)
                .Include(t => t.CreatedBy)
                .Include(t => t.TransferItems).ThenInclude(ti => ti.Product)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}
