using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagement.Core.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required, MaxLength(50)]
        public string SKU { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Barcode { get; set; }

        public string? ImagePath { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }

        public int ReorderLevel { get; set; } = 10;

        [MaxLength(50)]
        public string? BatchNumber { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public int CategoryId { get; set; }
        public int UnitId { get; set; }

        // Navigation
        public Category Category { get; set; } = null!;
        public Unit Unit { get; set; } = null!;
        public ICollection<ProductSupplier> ProductSuppliers { get; set; } = new List<ProductSupplier>();
        public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    }

    public class Category
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public class Unit
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(10)]
        public string Abbreviation { get; set; } = string.Empty;

        // Navigation
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public class ProductSupplier
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int SupplierId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SupplierPrice { get; set; }

        public bool IsPrimarySupplier { get; set; }

        // Navigation
        public Product Product { get; set; } = null!;
        public Supplier Supplier { get; set; } = null!;
    }
}
