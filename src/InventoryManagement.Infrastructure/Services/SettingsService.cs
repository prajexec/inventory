using Microsoft.EntityFrameworkCore;
using InventoryManagement.Core.Models;
using InventoryManagement.Infrastructure.Data;

namespace InventoryManagement.Infrastructure.Services
{
    public class SettingsService
    {
        private readonly ApplicationDbContext _db;

        public SettingsService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<SystemSetting>> GetAllSettingsAsync()
        {
            return await _db.SystemSettings.OrderBy(s => s.Key).ToListAsync();
        }

        public async Task<string?> GetSettingAsync(string key)
        {
            var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            return setting?.Value;
        }

        public async Task<decimal> GetTaxRateAsync()
        {
            var val = await GetSettingAsync("TaxRate");
            return decimal.TryParse(val, out var rate) ? rate : 18m;
        }

        public async Task<string> GetCurrencySymbolAsync()
        {
            return await GetSettingAsync("CurrencySymbol") ?? "₹";
        }

        public async Task SetSettingAsync(string key, string value)
        {
            var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting != null)
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.SystemSettings.Add(new SystemSetting
                {
                    Key = key,
                    Value = value,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }

        public async Task UpdateSettingsAsync(Dictionary<string, string> settings)
        {
            foreach (var kvp in settings)
            {
                await SetSettingAsync(kvp.Key, kvp.Value);
            }
        }
    }
}
