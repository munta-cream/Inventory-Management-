using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_Requirements.Data;
using Inventory_Management_Requirements.Models;

namespace Inventory_Management_Requirements.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public StatisticsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: Statistics
        public async Task<IActionResult> Index(int inventoryId)
        {
            var inventory = await _db.Inventories
                .Include(i => i.Items)
                .Include(i => i.Category)
                .FirstOrDefaultAsync(i => i.Id == inventoryId);
            
            if (inventory == null)
            {
                return NotFound();
            }
            
            return View(inventory);
        }
    }
}
