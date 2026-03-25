using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Models;
using InventoryManagement.Infrastructure.Data;

namespace InventoryManagement.Infrastructure.Services
{
    public class SupplierService
    {
        private readonly ApplicationDbContext _db;

        public SupplierService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<Supplier>> GetSuppliersAsync(string? search = null)
        {
            var query = _db.Suppliers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(search) ||
                    (s.ContactPerson != null && s.ContactPerson.ToLower().Contains(search)) ||
                    (s.Email != null && s.Email.ToLower().Contains(search)));
            }

            return await query.OrderBy(s => s.Name).ToListAsync();
        }

        public async Task<Supplier?> GetSupplierByIdAsync(int id)
        {
            return await _db.Suppliers
                .Include(s => s.ProductSuppliers).ThenInclude(ps => ps.Product)
                .Include(s => s.PurchaseOrders)
                .Include(s => s.SupplierPayments)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<(bool success, string? error)> SaveSupplierAsync(Supplier supplier)
        {
            if (supplier.Id == 0)
            {
                supplier.CreatedAt = DateTime.UtcNow;
                _db.Suppliers.Add(supplier);
            }
            else
            {
                var existing = await _db.Suppliers.FindAsync(supplier.Id);
                if (existing == null) return (false, "Supplier not found.");

                existing.Name = supplier.Name;
                existing.ContactPerson = supplier.ContactPerson;
                existing.Email = supplier.Email;
                existing.Phone = supplier.Phone;
                existing.Address = supplier.Address;
                existing.City = supplier.City;
                existing.Country = supplier.Country;
                existing.Rating = supplier.Rating;
                existing.IsActive = supplier.IsActive;
            }
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> DeleteSupplierAsync(int id)
        {
            var supplier = await _db.Suppliers.FindAsync(id);
            if (supplier == null) return false;
            supplier.IsActive = false;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetOutstandingBalanceAsync(int supplierId)
        {
            var orders = await _db.PurchaseOrders
                .Where(po => po.SupplierId == supplierId && po.Status != Core.Enums.OrderStatus.Cancelled)
                .ToListAsync();
            var totalOrdered = orders.Sum(po => po.TotalAmount);

            var payments = await _db.SupplierPayments
                .Where(sp => sp.SupplierId == supplierId)
                .ToListAsync();
            var totalPaid = payments.Sum(sp => sp.Amount);

            return totalOrdered - totalPaid;
        }

        public async Task<(bool success, string? error)> RecordPaymentAsync(SupplierPayment payment)
        {
            payment.PaymentDate = DateTime.UtcNow;
            _db.SupplierPayments.Add(payment);
            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<List<SupplierPayment>> GetPaymentsAsync(int supplierId)
        {
            return await _db.SupplierPayments
                .Where(sp => sp.SupplierId == supplierId)
                .OrderByDescending(sp => sp.PaymentDate)
                .ToListAsync();
        }
    }
}
