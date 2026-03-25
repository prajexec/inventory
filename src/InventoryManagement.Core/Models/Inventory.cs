using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InventoryManagement.Core.Enums;

namespace InventoryManagement.Core.Models
{
    public class Inventory
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public int Quantity { get; set; } = 0;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation
        public Product Product { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
    }

    public class StockMovement
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }

        public MovementType MovementType { get; set; }
        public int Quantity { get; set; }
        public int PreviousQuantity { get; set; }
        public int NewQuantity { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        [MaxLength(50)]
        public string? ReferenceNumber { get; set; }

        public int? PerformedByUserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation
        public Product Product { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
        public User? PerformedBy { get; set; }
    }
}
