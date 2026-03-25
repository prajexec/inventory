using System.ComponentModel.DataAnnotations;
using InventoryManagement.Core.Enums;

namespace InventoryManagement.Core.Models
{
    public class Warehouse
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        public bool IsActive { get; set; } = true;

        public int? ManagerUserId { get; set; }

        // Navigation
        public User? Manager { get; set; }
        public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }

    public class WarehouseTransfer
    {
        public int Id { get; set; }

        [MaxLength(50)]
        public string TransferNumber { get; set; } = string.Empty;

        public int FromWarehouseId { get; set; }
        public int ToWarehouseId { get; set; }

        public TransferStatus Status { get; set; } = TransferStatus.Pending;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Navigation
        public Warehouse FromWarehouse { get; set; } = null!;
        public Warehouse ToWarehouse { get; set; } = null!;
        public User CreatedBy { get; set; } = null!;
        public ICollection<TransferItem> TransferItems { get; set; } = new List<TransferItem>();
    }

    public class TransferItem
    {
        public int Id { get; set; }
        public int WarehouseTransferId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        // Navigation
        public WarehouseTransfer WarehouseTransfer { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
