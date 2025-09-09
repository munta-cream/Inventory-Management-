using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Inventory_Management_Requirements.Models;

namespace Inventory_Management_Requirements.Services
{
    public class SearchService
    {
        private readonly ApplicationDbContext _db;

        public SearchService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<Inventory>> SearchInventoriesAsync(string searchTerm)
        {
            return await _db.Inventories
                .Where(i => i.Title.Contains(searchTerm) || i.Description.Contains(searchTerm))
                .ToListAsync();
        }

        public async Task<List<Item>> SearchItemsAsync(string searchTerm)
        {
            // Search in CustomId and FieldData (JSON)
            return await _db.Items
                .Where(i => i.CustomId.Contains(searchTerm) ||
                           i.FieldData.Contains(searchTerm))
                .Include(i => i.Inventory)
                .ToListAsync();
        }

        public async Task<List<Inventory>> SearchInventoriesByCategoryAsync(string categoryName)
        {
            return await _db.Inventories
                .Include(i => i.Category)
                .Where(i => i.Category.Name.Contains(categoryName))
                .ToListAsync();
        }

        public async Task<List<Item>> SearchItemsByInventoryAsync(string searchTerm, int inventoryId)
        {
            return await _db.Items
                .Where(i => i.InventoryId == inventoryId &&
                           (i.CustomId.Contains(searchTerm) || i.FieldData.Contains(searchTerm)))
                .ToListAsync();
        }

        public async Task<List<Inventory>> GetRecentInventoriesAsync(int count = 10)
        {
            return await _db.Inventories
                .Include(i => i.CreatedBy)
                .Include(i => i.Category)
                .OrderByDescending(i => i.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
