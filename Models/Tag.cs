using System.ComponentModel.DataAnnotations;

namespace Inventory_Management_Requirements.Models
{
    public class Tag
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; }
        
        public int UsageCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
        
        public virtual ICollection<InventoryTag> InventoryTags { get; set; }
    }
}
