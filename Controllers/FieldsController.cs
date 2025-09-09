using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_Requirements.Data;
using Inventory_Management_Requirements.Models;

namespace Inventory_Management_Requirements.Controllers
{
    public class FieldsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public FieldsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: Fields
        public async Task<IActionResult> Index(int inventoryId)
        {
            var fields = await _db.CustomFields
                .Where(f => f.InventoryId == inventoryId)
                .OrderBy(f => f.Order)
                .ToListAsync();
            ViewBag.InventoryId = inventoryId;
            return View(fields);
        }

        // GET: Fields/Create
        public IActionResult Create(int inventoryId)
        {
            var field = new CustomField { InventoryId = inventoryId };
            return View(field);
        }

        // POST: Fields/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomField field)
        {
            if (ModelState.IsValid)
            {
                // Set the order to be the last in the list
                var maxOrder = await _db.CustomFields
                    .Where(f => f.InventoryId == field.InventoryId)
                    .MaxAsync(f => (int?)f.Order) ?? 0;
                field.Order = maxOrder + 1;

                _db.CustomFields.Add(field);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { inventoryId = field.InventoryId });
            }
            return View(field);
        }

        // GET: Fields/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var field = await _db.CustomFields.FindAsync(id);
            if (field == null)
            {
                return NotFound();
            }
            return View(field);
        }

        // POST: Fields/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomField field)
        {
            if (id != field.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _db.Update(field);
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FieldExists(field.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index), new { inventoryId = field.InventoryId });
            }
            return View(field);
        }

        // POST: Fields/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var field = await _db.CustomFields.FindAsync(id);
            if (field == null)
            {
                return NotFound();
            }

            _db.CustomFields.Remove(field);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { inventoryId = field.InventoryId });
        }

        // POST: Fields/Reorder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int inventoryId, List<int> fieldIds)
        {
            var fields = await _db.CustomFields
                .Where(f => f.InventoryId == inventoryId && fieldIds.Contains(f.Id))
                .ToListAsync();

            for (int i = 0; i < fieldIds.Count; i++)
            {
                var field = fields.FirstOrDefault(f => f.Id == fieldIds[i]);
                if (field != null)
                {
                    field.Order = i;
                }
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        private bool FieldExists(int id)
        {
            return _db.CustomFields.Any(e => e.Id == id);
        }
    }
}
