using System.Text.Json;

namespace Inventory_Management_Requirements.Models
{
    public class CustomIdFormat
    {
        public int Id { get; set; }
        public int InventoryId { get; set; }
        public virtual Inventory Inventory { get; set; }
        
        // JSON structure: array of format elements
        // Example: [{"type":"fixed","value":"ITEM-"},{"type":"seq","pad":4}]
        public string FormatDefinition { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public JsonDocument FormatDefinitionJson
        {
            get => string.IsNullOrEmpty(FormatDefinition) ? null : JsonDocument.Parse(FormatDefinition);
            set => FormatDefinition = value?.RootElement.GetRawText();
        }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // Format element types for custom ID generation
    public class FormatElement
    {
        public string Type { get; set; } // fixed, random, guid, datetime, seq
        public string Value { get; set; } // for fixed text
        public int? Pad { get; set; } // for sequence padding
        public string Format { get; set; } // for datetime formatting
        public int? Bits { get; set; } // for random number bits (20, 32, etc.)
        public int? Digits { get; set; } // for random number digits (6-digit, etc.)
    }
}
