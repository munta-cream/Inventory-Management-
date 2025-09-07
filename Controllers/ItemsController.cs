using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Inventory_Management_Requirements.Data;
using Inventory_Management_Requirements.Models;
using Inventory_Management_Requirements.Services;

public class ItemsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICustomIdGenerator _idGenerator;

    public ItemsController(ApplicationDbContext db, ICustomIdGenerator idGenerator)
    {
        _db = db;
        _idGenerator = idGenerator;
    }

    [HttpGet]
    public IActionResult Create(int inventoryId)
    {
        var inventory = _db.Inventories
            .Include(i => i.Fields)
            .FirstOrDefault(i => i.Id == inventoryId);

        if (inventory == null) return NotFound();

        ViewBag.Inventory = inventory;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int inventoryId, Item item)
    {
        if (!User.Identity.IsAuthenticated) return Forbid();

        var inventory = await _db.Inventories
            .Include(i => i.IdFormat)
            .FirstOrDefaultAsync(i => i.Id == inventoryId);

        if (inventory == null) return NotFound();

        // Generate custom ID
        var itemCount = await _db.Items.CountAsync(i => i.InventoryId == inventoryId);
        var formatJson = JsonDocument.Parse(inventory.IdFormat.FormatDefinition);
        item.CustomId = _idGenerator.Generate(inventoryId, formatJson, itemCount + 1);

        item.InventoryId = inventoryId;
        item.CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;

        if (ModelState.IsValid)
        {
            _db.Items.Add(item);
            await _db.SaveChangesAsync();
            return RedirectToAction("Details", "Inventories", new { id = inventoryId });
        }

        ViewBag.Inventory = inventory;
        return View(item);
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var item = _db.Items
            .Include(i => i.Inventory)
            .ThenInclude(i => i.Fields)
            .FirstOrDefault(i => i.Id == id);

        if (item == null) return NotFound();

        ViewBag.Inventory = item.Inventory;
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Item item)
    {
        if (!User.Identity.IsAuthenticated) return Forbid();

        var existingItem = await _db.Items
            .Include(i => i.Inventory)
            .FirstOrDefaultAsync(i => i.Id == item.Id);

        if (existingItem == null) return NotFound();

        existingItem.FieldData = item.FieldData;
        existingItem.UpdatedAt = DateTime.UtcNow;

        if (ModelState.IsValid)
        {
            _db.Items.Update(existingItem);
            await _db.SaveChangesAsync();
            return RedirectToAction("Details", "Inventories", new { id = existingItem.InventoryId });
        }

        ViewBag.Inventory = existingItem.Inventory;
        return View(existingItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Items.FindAsync(id);
        if (item == null) return NotFound();

        var inventoryId = item.InventoryId;
        _db.Items.Remove(item);
        await _db.SaveChangesAsync();
        return RedirectToAction("Details", "Inventories", new { id = inventoryId });
    }
}
