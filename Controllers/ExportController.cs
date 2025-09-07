using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory_Management_Requirements.Data;
using Inventory_Management_Requirements.Models;
using System.Text;
using System.Text.Json;

namespace Inventory_Management_Requirements.Controllers
{
    public class ExportController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ExportController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: Export
        public async Task<IActionResult> Index(int inventoryId)
        {
            var inventory = await _db.Inventories
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inventory == null)
            {
                return NotFound();
            }

            return View(inventory);
        }

        // GET: Export/ExportCsv
        public async Task<IActionResult> ExportCsv(int inventoryId)
        {
            var inventory = await _db.Inventories
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inventory == null)
            {
                return NotFound();
            }

            var csv = new StringBuilder();
            csv.AppendLine("CustomId,CreatedAt,FieldData");

            foreach (var item in inventory.Items)
            {
                var fieldData = item.FieldData ?? "{}";
                csv.AppendLine($"{item.CustomId},{item.CreatedAt},{fieldData}");
            }

            var fileName = $"{inventory.Title}_Export.csv";
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        // GET: Export/ExportExcel
        public async Task<IActionResult> ExportExcel(int inventoryId)
        {
            var inventory = await _db.Inventories
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inventory == null)
            {
                return NotFound();
            }

            // For simplicity, we'll export as CSV with .xlsx extension
            // In a real application, you would use a library like EPPlus or ClosedXML
            var csv = new StringBuilder();
            csv.AppendLine("CustomId,CreatedAt,FieldData");

            foreach (var item in inventory.Items)
            {
                var fieldData = item.FieldData ?? "{}";
                csv.AppendLine($"{item.CustomId},{item.CreatedAt},{fieldData}");
            }

            var fileName = $"{inventory.Title}_Export.xlsx";
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
