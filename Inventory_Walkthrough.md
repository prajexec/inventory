Inventory Management System — Walkthrough

## What Was Built
A streamlined ASP.NET Core 8.0 MVC app utilizing a 3-tier architecture (Core, Infrastructure, Web) with 8 database tables, cookie authentication, and full CRUD operations for products, suppliers, and purchase orders.

## Project Structure
```text
InventoryManagement/
├── src/
│   ├── InventoryManagement.Core/
│   │   └── Models/              ← Product.cs, Supplier.cs, PurchaseOrder.cs, User.cs
│   │
│   ├── InventoryManagement.Infrastructure/
│   │   ├── Data/
│   │   │   └── ApplicationDbContext.cs ← EF Core DbContext, 8 DbSets, seeds admin & sample data
│   │   └── Services/            ← ProductService.cs, SupplierService.cs, PurchaseService.cs, AuthService.cs
│   │
│   └── InventoryManagement.Web/
│       ├── Controllers/         ← AccountController, DashboardController, ProductController, etc.
│       ├── Views/               ← Razor views for all controllers (Shared, Account, Product, etc.)
│       └── Program.cs           ← SQLite setup + cookie auth + DI configuration
└── InventoryManagement.sln
```

## Key Concepts Covered

| Concept | Where |
|---------|-------|
| 3-Tier Architecture | `Core`, `Infrastructure`, `Web` projects |
| Models & properties | `Product.cs`, `PurchaseOrder.cs`, `Supplier.cs` |
| Foreign keys & navigation | `Product.CategoryId`, `PurchaseOrder.SupplierId` |
| EF Core DbContext & seed data | `ApplicationDbContext.cs` |
| `Include()`, `FirstOrDefaultAsync`, `SaveChangesAsync` | `ProductService.cs`, `PurchaseService.cs` |
| Cookie authentication & Dependency Injection | `Program.cs` + `AccountController.cs` |
| BCrypt password hashing | `AuthService.cs` + Admin seed in `Program.cs` |
| `[Authorize]` attribute | Filtered access in all main Controllers |
| GET/POST pattern | Every controller action (`Create`, `Edit`, `Delete`) |
| Razor views + tag helpers | All `.cshtml` templates |

## How to Run

```bash
cd /Users/priyanshu/cSharp/inventory/src/InventoryManagement.Web
dotnet run
```

Then open `http://localhost:5000` (or the URL printed in your terminal) and login with:

- **Username:** `admin@inventory.com`
- **Password:** `Admin@123`

## Build Verification
✅ `dotnet build` — 0 errors, 0 warnings
