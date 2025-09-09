using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Inventory_Management_Requirements.Data;
using Inventory_Management_Requirements.Models;
using Inventory_Management_Requirements.Services;

public class CustomIdController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICustomIdGenerator _idGen;

    public CustomIdController(ApplicationDbContext db, ICustomIdGenerator idGen)
    {
        _db = db;
        _idGen = idGen;
    }

    public IActionResult Index(int inventoryId)
    {
        var format = _db.CustomIdFormats.FirstOrDefault(f => f.InventoryId == inventoryId);
        return View(format);
    }

    [HttpPost]
    public IActionResult SaveFormat(int inventoryId, string formatJson)
    {
        var existing = _db.CustomIdFormats.FirstOrDefault(f => f.InventoryId == inventoryId);
        if (existing != null)
        {
            existing.FormatDefinition = formatJson;
            _db.CustomIdFormats.Update(existing);
        }
        else
        {
            _db.CustomIdFormats.Add(new CustomIdFormat
            {
                InventoryId = inventoryId,
                FormatDefinition = formatJson
            });
        }
        _db.SaveChanges();
        return Ok();
    }

    [HttpGet]
    public IActionResult Preview(int inventoryId, string formatJson)
    {
        var format = JsonDocument.Parse(formatJson);
        var count = _db.Items.Count(i => i.InventoryId == inventoryId);
        var id = _idGen.Generate(inventoryId, format, count + 1);
        return Json(new { id });
    }
}
