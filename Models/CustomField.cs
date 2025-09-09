using System.ComponentModel.DataAnnotations;

namespace Inventory_Management_Requirements.Models
{
    public class CustomField
    {
        public int Id { get; set; }
        public int InventoryId { get; set; }
        public virtual Inventory Inventory { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; }
        
        [StringLength(500)]
        public string Description { get; set; }
        
        [Required]
        public FieldType Type { get; set; }
        
        public int Order { get; set; }
        public bool ShowInTableView { get; set; } = true;
        
        // Field-specific validation properties
        public int? MaxLength { get; set; }
        public bool? IsRequired { get; set; }
        public string? ValidationRegex { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum FieldType
    {
        SingleLineText,
        MultiLineText,
        Number,
        Boolean,
        DocumentLink,
        ImageLink,
        // Optional: Dropdown (for future enhancement)
        Dropdown
    }
}
