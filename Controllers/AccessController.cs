using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_Requirements.Data;
using Inventory_Management_Requirements.Models;

namespace Inventory_Management_Requirements.Controllers
{
    public class AccessController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AccessController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: Access
        public async Task<IActionResult> Index(int inventoryId)
        {
            var accessList = await _db.InventoryAccesses
                .Include(a => a.User)
                .Where(a => a.InventoryId == inventoryId)
                .ToListAsync();
            
            // Get all users except those already in the access list
            var existingUserIds = accessList.Select(a => a.UserId).ToList();
            var users = await _db.Users
                .Where(u => !existingUserIds.Contains(u.Id))
                .ToListAsync();
            
            ViewBag.Users = users;
            ViewBag.InventoryId = inventoryId;
            return View(accessList);
        }

        // POST: Access/AddAccess
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAccess(int inventoryId, string userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var existingAccess = await _db.InventoryAccesses
                .FirstOrDefaultAsync(a => a.InventoryId == inventoryId && a.UserId == userId);

            if (existingAccess != null)
            {
                ModelState.AddModelError("", "User already has access to this inventory.");
                return RedirectToAction(nameof(Index), new { inventoryId });
            }

            var access = new InventoryAccess
            {
                InventoryId = inventoryId,
                UserId = userId,
                GrantedById = User.Identity.Name // This should be the current user's ID
            };

            _db.InventoryAccesses.Add(access);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { inventoryId });
        }

        // POST: Access/RemoveAccess/5
        [HttpPost, ActionName("RemoveAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAccess(int id)
        {
            var access = await _db.InventoryAccesses.FindAsync(id);
            if (access == null)
            {
                return NotFound();
            }

            _db.InventoryAccesses.Remove(access);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { inventoryId = access.InventoryId });
        }
    }
}
