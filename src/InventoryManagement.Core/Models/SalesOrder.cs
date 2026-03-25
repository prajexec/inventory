using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InventoryManagement.Core.Enums;

namespace InventoryManagement.Core.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
    }

    public class SalesOrder
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        public int? CustomerId { get; set; }
        public int WarehouseId { get; set; }

        public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(5,2)")]
        public decimal TaxPercent { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public int CreatedByUserId { get; set; }

        // Navigation
        public Customer? Customer { get; set; }
        public Warehouse Warehouse { get; set; } = null!;
        public User CreatedBy { get; set; } = null!;
        public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public class SalesOrderItem
    {
        public int Id { get; set; }
        public int SalesOrderId { get; set; }
        public int ProductId { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public int ReturnedQuantity { get; set; } = 0;

        // Navigation
        public SalesOrder SalesOrder { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }

    public class Payment
    {
        public int Id { get; set; }
        public int SalesOrderId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        // Navigation
        public SalesOrder SalesOrder { get; set; } = null!;
    }
}
