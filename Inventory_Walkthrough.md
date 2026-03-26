# Inventory Management System — Walkthrough

## What Was Built
A structured 3-tier ASP.NET Core 8.0 MVC application (Core, Infrastructure, Web) utilizing a SQLite database. It features cookie-based authentication with role-based policies and full CRUD operations for managing products, inventory, suppliers, sales orders, and purchase orders.

## Project Structure
```text
InventoryManagement/
├── src/
│   ├── InventoryManagement.Core/        ← Domain Layer
│   │   └── Models/                      ← Product, Inventory, Supplier, User, SalesOrder, etc.
│   │
│   ├── InventoryManagement.Infrastructure/ ← Data Access & Business Logic
│   │   ├── Data/ApplicationDbContext.cs ← EF Core DbContext
│   │   └── Services/                    ← ProductService, AuthService, SalesService, etc.
│   │
│   └── InventoryManagement.Web/         ← Presentation Layer
│       ├── Controllers/                 ← AccountController, DashboardController, ProductController, etc.
│       ├── Views/                       ← Razor views for all controllers
│       └── Program.cs                   ← SQLite setup, Cookie Auth, DI container
```

## Key Concepts Covered

| Concept | Where |
|---------|-------|
| 3-Tier Architecture | `Core`, `Infrastructure`, `Web` projects |
| Models & properties | `Product.cs`, `Inventory.cs`, `Supplier.cs`, etc. |
| Foreign keys & navigation | e.g. `SalesOrder` relationships to `Product` and `User` |
| EF Core DbContext & seed data | `ApplicationDbContext.cs` & `Program.cs` |
| Dependency Injection (DI) | `Program.cs` (`AddScoped<ProductService>()`) |
| Cookie authentication | `AccountController.cs` & `Program.cs` |
| Role-based Policies | `Program.cs` (`RequireRole("Admin")`) |
| BCrypt password hashing | `Program.cs` (admin seed) & auth logic |
| MVC Pattern | All `Controllers/` and `Views/` |

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
