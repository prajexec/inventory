using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InventoryManagement.Core.Enums;

namespace InventoryManagement.Core.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? ContactPerson { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        public int Rating { get; set; } = 3; // 1-5

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<ProductSupplier> ProductSuppliers { get; set; } = new List<ProductSupplier>();
        public ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();
        public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    }

    public class SupplierPayment
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public int? PurchaseOrderId { get; set; }

        // Navigation
        public Supplier Supplier { get; set; } = null!;
        public PurchaseOrder? PurchaseOrder { get; set; }
    }
}
