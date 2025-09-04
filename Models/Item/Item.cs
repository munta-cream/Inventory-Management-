using System;
using System.Text.Json;

namespace Inventory_Management_Requirements.Models
{
    public class Item
    {
        public int Id { get; set; }
        public int InventoryId { get; set; }
        public virtual Inventory Inventory { get; set; }
        public string CustomId { get; set; }
        public string CreatedById { get; set; }
        public virtual ApplicationUser CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // JSONB: { "serial": "A1", "pages": 200, "active": true }
        public string FieldData { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public JsonDocument FieldDataJson
        {
            get => string.IsNullOrEmpty(FieldData) ? null : JsonDocument.Parse(FieldData);
            set => FieldData = value?.RootElement.GetRawText();
        }
    }
}
