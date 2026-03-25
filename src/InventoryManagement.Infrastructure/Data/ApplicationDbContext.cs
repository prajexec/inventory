using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Models;
using InventoryManagement.Core.Enums;

namespace InventoryManagement.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Auth
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<LoginLog> LoginLogs => Set<LoginLog>();

        // Products
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Unit> Units => Set<Unit>();
        public DbSet<ProductSupplier> ProductSuppliers => Set<ProductSupplier>();

        // Inventory
        public DbSet<Inventory> Inventories => Set<Inventory>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();

        // Warehouse
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<WarehouseTransfer> WarehouseTransfers => Set<WarehouseTransfer>();
        public DbSet<TransferItem> TransferItems => Set<TransferItem>();

        // Suppliers
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();

        // Purchases
        public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
        public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
        public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();

        // Sales
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
        public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
        public DbSet<Payment> Payments => Set<Payment>();

        // System
        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Indexes =====
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email).IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SKU).IsUnique();

            modelBuilder.Entity<Inventory>()
                .HasIndex(i => new { i.ProductId, i.WarehouseId }).IsUnique();

            modelBuilder.Entity<SystemSetting>()
                .HasIndex(s => s.Key).IsUnique();

            // ===== Relationships =====

            // UserRole
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // LoginLog
            modelBuilder.Entity<LoginLog>()
                .HasOne(l => l.User)
                .WithMany(u => u.LoginLogs)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Product
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Unit)
                .WithMany(u => u.Products)
                .HasForeignKey(p => p.UnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // ProductSupplier
            modelBuilder.Entity<ProductSupplier>()
                .HasOne(ps => ps.Product)
                .WithMany(p => p.ProductSuppliers)
                .HasForeignKey(ps => ps.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductSupplier>()
                .HasOne(ps => ps.Supplier)
                .WithMany(s => s.ProductSuppliers)
                .HasForeignKey(ps => ps.SupplierId)
                .OnDelete(DeleteBehavior.Cascade);

            // Inventory
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Inventories)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Warehouse)
                .WithMany(w => w.Inventories)
                .HasForeignKey(i => i.WarehouseId)
                .OnDelete(DeleteBehavior.Cascade);

            // StockMovement
            modelBuilder.Entity<StockMovement>()
                .HasOne(sm => sm.Product)
                .WithMany()
                .HasForeignKey(sm => sm.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StockMovement>()
                .HasOne(sm => sm.Warehouse)
                .WithMany()
                .HasForeignKey(sm => sm.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Warehouse
            modelBuilder.Entity<Warehouse>()
                .HasOne(w => w.Manager)
                .WithMany()
                .HasForeignKey(w => w.ManagerUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // WarehouseTransfer
            modelBuilder.Entity<WarehouseTransfer>()
                .HasOne(wt => wt.FromWarehouse)
                .WithMany()
                .HasForeignKey(wt => wt.FromWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WarehouseTransfer>()
                .HasOne(wt => wt.ToWarehouse)
                .WithMany()
                .HasForeignKey(wt => wt.ToWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // PurchaseOrder
            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(po => po.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.Warehouse)
                .WithMany()
                .HasForeignKey(po => po.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // PurchaseInvoice - one to one with PO
            modelBuilder.Entity<PurchaseInvoice>()
                .HasOne(pi => pi.PurchaseOrder)
                .WithOne(po => po.Invoice)
                .HasForeignKey<PurchaseInvoice>(pi => pi.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // SalesOrder
            modelBuilder.Entity<SalesOrder>()
                .HasOne(so => so.Customer)
                .WithMany(c => c.SalesOrders)
                .HasForeignKey(so => so.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalesOrder>()
                .HasOne(so => so.Warehouse)
                .WithMany()
                .HasForeignKey(so => so.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== Seed Data =====

            // Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", Description = "Full system access" },
                new Role { Id = 2, Name = "Manager", Description = "Manage inventory and staff" },
                new Role { Id = 3, Name = "Staff", Description = "Basic operations" }
            );

            // Admin user (password: Admin@123)
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    FullName = "System Administrator",
                    Email = "admin@inventory.com",
                    PasswordHash = "$2a$11$K7rFz.VOqSbzPJLJmQ3jH.J5pNpRZzV1yGORl5qL4Wz1VpfPgPWqG",
                    IsActive = true,
                    CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { Id = 1, UserId = 1, RoleId = 1 }
            );

            // Units
            modelBuilder.Entity<Unit>().HasData(
                new Unit { Id = 1, Name = "Piece", Abbreviation = "pcs" },
                new Unit { Id = 2, Name = "Kilogram", Abbreviation = "kg" },
                new Unit { Id = 3, Name = "Liter", Abbreviation = "L" },
                new Unit { Id = 4, Name = "Meter", Abbreviation = "m" },
                new Unit { Id = 5, Name = "Box", Abbreviation = "box" },
                new Unit { Id = 6, Name = "Carton", Abbreviation = "ctn" },
                new Unit { Id = 7, Name = "Dozen", Abbreviation = "dz" }
            );

            // Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Electronics", Description = "Electronic devices and accessories" },
                new Category { Id = 2, Name = "Office Supplies", Description = "Stationery and office materials" },
                new Category { Id = 3, Name = "Hardware", Description = "Tools and hardware components" },
                new Category { Id = 4, Name = "Raw Materials", Description = "Manufacturing raw materials" },
                new Category { Id = 5, Name = "Packaging", Description = "Packaging materials" }
            );

            // Default Warehouses
            modelBuilder.Entity<Warehouse>().HasData(
                new Warehouse { Id = 1, Name = "Main Warehouse", Address = "123 Industrial Park", City = "Mumbai", IsActive = true, ManagerUserId = 1 },
                new Warehouse { Id = 2, Name = "Secondary Warehouse", Address = "456 Commerce Road", City = "Delhi", IsActive = true }
            );

            // System Settings
            modelBuilder.Entity<SystemSetting>().HasData(
                new SystemSetting { Id = 1, Key = "TaxRate", Value = "18", Description = "Default tax percentage", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new SystemSetting { Id = 2, Key = "Currency", Value = "INR", Description = "Default currency code", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new SystemSetting { Id = 3, Key = "CurrencySymbol", Value = "₹", Description = "Default currency symbol", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new SystemSetting { Id = 4, Key = "DefaultWarehouseId", Value = "1", Description = "Default warehouse for operations", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new SystemSetting { Id = 5, Key = "LowStockThreshold", Value = "10", Description = "Global low stock threshold", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new SystemSetting { Id = 6, Key = "CompanyName", Value = "Inventory Pro Inc.", Description = "Company name for reports", UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // Sample Suppliers
            modelBuilder.Entity<Supplier>().HasData(
                new Supplier { Id = 1, Name = "TechParts India", ContactPerson = "Rahul Sharma", Email = "rahul@techparts.in", Phone = "+91-9876543210", Address = "789 Tech Park", City = "Bangalore", Country = "India", Rating = 4, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Supplier { Id = 2, Name = "Office World", ContactPerson = "Priya Patel", Email = "priya@officeworld.in", Phone = "+91-9876543211", Address = "321 Business Center", City = "Mumbai", Country = "India", Rating = 5, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Supplier { Id = 3, Name = "Raw Materials Co.", ContactPerson = "Amit Kumar", Email = "amit@rawmat.in", Phone = "+91-9876543212", Address = "654 Industrial Zone", City = "Chennai", Country = "India", Rating = 3, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // Sample Products
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Wireless Mouse", Description = "Ergonomic wireless mouse with USB receiver", SKU = "ELEC-001", Barcode = "8901234567890", CostPrice = 350m, SellingPrice = 599m, ReorderLevel = 20, CategoryId = 1, UnitId = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Product { Id = 2, Name = "Mechanical Keyboard", Description = "RGB mechanical keyboard with blue switches", SKU = "ELEC-002", Barcode = "8901234567891", CostPrice = 1200m, SellingPrice = 2499m, ReorderLevel = 15, CategoryId = 1, UnitId = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Product { Id = 3, Name = "A4 Paper Ream", Description = "500 sheets of premium A4 copy paper", SKU = "OFF-001", CostPrice = 180m, SellingPrice = 299m, ReorderLevel = 50, CategoryId = 2, UnitId = 5, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Product { Id = 4, Name = "USB-C Cable", Description = "1m braided USB-C charging cable", SKU = "ELEC-003", CostPrice = 80m, SellingPrice = 199m, ReorderLevel = 30, CategoryId = 1, UnitId = 1, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Product { Id = 5, Name = "Hex Bolt Set M10", Description = "Set of 50 M10 hex bolts with nuts", SKU = "HW-001", CostPrice = 250m, SellingPrice = 450m, ReorderLevel = 25, CategoryId = 3, UnitId = 5, CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // Sample Inventory
            modelBuilder.Entity<Inventory>().HasData(
                new Inventory { Id = 1, ProductId = 1, WarehouseId = 1, Quantity = 150, LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Inventory { Id = 2, ProductId = 2, WarehouseId = 1, Quantity = 75, LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Inventory { Id = 3, ProductId = 3, WarehouseId = 1, Quantity = 200, LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Inventory { Id = 4, ProductId = 4, WarehouseId = 1, Quantity = 300, LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Inventory { Id = 5, ProductId = 5, WarehouseId = 1, Quantity = 8, LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Inventory { Id = 6, ProductId = 1, WarehouseId = 2, Quantity = 50, LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Inventory { Id = 7, ProductId = 2, WarehouseId = 2, Quantity = 30, LastUpdated = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );

            // Sample Customer
            modelBuilder.Entity<Customer>().HasData(
                new Customer { Id = 1, Name = "ABC Corp", Email = "purchasing@abccorp.in", Phone = "+91-1234567890", Address = "100 Corporate Ave", City = "Mumbai", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new Customer { Id = 2, Name = "XYZ Industries", Email = "orders@xyzind.in", Phone = "+91-1234567891", Address = "200 Industrial St", City = "Pune", CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );
        }
    }
}
