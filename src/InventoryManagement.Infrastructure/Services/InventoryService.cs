using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Models;
using InventoryManagement.Core.Enums;
using InventoryManagement.Infrastructure.Data;

namespace InventoryManagement.Infrastructure.Services
{
    public class InventoryService
    {
        private readonly ApplicationDbContext _db;

        public InventoryService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<Inventory>> GetInventoryAsync(int? warehouseId = null, string? search = null)
        {
            var query = _db.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Include(i => i.Product).ThenInclude(p => p.Unit)
                .Include(i => i.Warehouse)
                .AsQueryable();

            if (warehouseId.HasValue)
                query = query.Where(i => i.WarehouseId == warehouseId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(i =>
                    i.Product.Name.ToLower().Contains(search) ||
                    i.Product.SKU.ToLower().Contains(search));
            }

            return await query.OrderBy(i => i.Product.Name).ToListAsync();
        }

        public async Task<List<Inventory>> GetLowStockItemsAsync(int? threshold = null)
        {
            var query = _db.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Include(i => i.Warehouse)
                .Where(i => i.Quantity <= (threshold ?? i.Product.ReorderLevel))
                .OrderBy(i => i.Quantity);

            return await query.ToListAsync();
        }

        public async Task<(bool success, string? error)> AdjustStockAsync(
            int productId, int warehouseId, int adjustmentQty, string reason, int userId)
        {
            var inventory = await _db.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);

            if (inventory == null)
            {
                if (adjustmentQty < 0)
                    return (false, "Cannot reduce stock below zero for a new inventory record.");

                inventory = new Inventory
                {
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    Quantity = adjustmentQty,
                    LastUpdated = DateTime.UtcNow
                };
                _db.Inventories.Add(inventory);
            }
            else
            {
                var newQty = inventory.Quantity + adjustmentQty;
                if (newQty < 0)
                    return (false, $"Insufficient stock. Current: {inventory.Quantity}, Adjustment: {adjustmentQty}");

                var previousQty = inventory.Quantity;
                inventory.Quantity = newQty;
                inventory.LastUpdated = DateTime.UtcNow;

                _db.StockMovements.Add(new StockMovement
                {
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    MovementType = adjustmentQty > 0 ? MovementType.StockIn : MovementType.StockOut,
                    Quantity = Math.Abs(adjustmentQty),
                    PreviousQuantity = previousQty,
                    NewQuantity = newQty,
                    Reason = reason,
                    ReferenceNumber = $"ADJ-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    PerformedByUserId = userId,
                    Timestamp = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task AddStockAsync(int productId, int warehouseId, int quantity, string reference, int userId, MovementType movementType = MovementType.StockIn)
        {
            var inventory = await _db.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);

            var previousQty = inventory?.Quantity ?? 0;

            if (inventory == null)
            {
                inventory = new Inventory
                {
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    Quantity = quantity,
                    LastUpdated = DateTime.UtcNow
                };
                _db.Inventories.Add(inventory);
            }
            else
            {
                inventory.Quantity += quantity;
                inventory.LastUpdated = DateTime.UtcNow;
            }

            _db.StockMovements.Add(new StockMovement
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                MovementType = movementType,
                Quantity = quantity,
                PreviousQuantity = previousQty,
                NewQuantity = inventory.Quantity,
                ReferenceNumber = reference,
                PerformedByUserId = userId,
                Timestamp = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        public async Task<(bool success, string? error)> DeductStockAsync(int productId, int warehouseId, int quantity, string reference, int userId)
        {
            var inventory = await _db.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.WarehouseId == warehouseId);

            if (inventory == null || inventory.Quantity < quantity)
                return (false, $"Insufficient stock. Available: {inventory?.Quantity ?? 0}");

            var previousQty = inventory.Quantity;
            inventory.Quantity -= quantity;
            inventory.LastUpdated = DateTime.UtcNow;

            _db.StockMovements.Add(new StockMovement
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                MovementType = MovementType.StockOut,
                Quantity = quantity,
                PreviousQuantity = previousQty,
                NewQuantity = inventory.Quantity,
                ReferenceNumber = reference,
                PerformedByUserId = userId,
                Timestamp = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<StockMovement>> GetStockMovementsAsync(int? productId = null, int? warehouseId = null, int count = 100)
        {
            var query = _db.StockMovements
                .Include(sm => sm.Product)
                .Include(sm => sm.Warehouse)
                .Include(sm => sm.PerformedBy)
                .AsQueryable();

            if (productId.HasValue) query = query.Where(sm => sm.ProductId == productId.Value);
            if (warehouseId.HasValue) query = query.Where(sm => sm.WarehouseId == warehouseId.Value);

            return await query.OrderByDescending(sm => sm.Timestamp).Take(count).ToListAsync();
        }

        public async Task<int> GetTotalStockAsync(int productId)
        {
            return await _db.Inventories
                .Where(i => i.ProductId == productId)
                .SumAsync(i => i.Quantity);
        }
    }
}
