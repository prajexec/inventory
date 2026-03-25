using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InventoryManagement.Core.Enums;

namespace InventoryManagement.Core.Models
{
    public class PurchaseOrder
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        public int SupplierId { get; set; }
        public int WarehouseId { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime? DeliveredDate { get; set; }

        public int CreatedByUserId { get; set; }

        // Navigation
        public Supplier Supplier { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
        public User CreatedBy { get; set; } = null!;
        public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
        public PurchaseInvoice? Invoice { get; set; }
    }

    public class PurchaseOrderItem
    {
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; }
        public int ProductId { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public int ReceivedQuantity { get; set; } = 0;

        // Navigation
        public PurchaseOrder PurchaseOrder { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }

    public class PurchaseInvoice
    {
        public int Id { get; set; }
        public int PurchaseOrderId { get; set; }

        [Required, MaxLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }

        // Navigation
        public PurchaseOrder PurchaseOrder { get; set; } = null!;
    }
}
