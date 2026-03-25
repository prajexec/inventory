using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using InventoryManagement.Infrastructure.Data;
using InventoryManagement.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=inventory.db"));

// Authentication
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Admin", "Manager"));
});

// Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<SupplierService>();
builder.Services.AddScoped<PurchaseService>();
builder.Services.AddScoped<SalesService>();
builder.Services.AddScoped<WarehouseService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ActivityLogService>();
builder.Services.AddScoped<SettingsService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Auto-migrate and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();

    // Always ensure admin has correct password and is unlocked
    var admin = db.Users.FirstOrDefault(u => u.Email == "admin@inventory.com");
    if (admin != null)
    {
        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
        admin.IsLocked = false;
        admin.FailedLoginAttempts = 0;
        admin.LockoutEnd = null;
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
