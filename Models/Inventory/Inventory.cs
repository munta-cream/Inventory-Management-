using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Inventory_Management_Requirements.Models
{
    public class Inventory
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty; // Markdown

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public virtual Category? Category { get; set; }

        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public string CreatedById { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public virtual ApplicationUser? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<CustomField> Fields { get; set; } = new List<CustomField>();
        [System.Text.Json.Serialization.JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public virtual CustomIdFormat? IdFormat { get; set; }
        public virtual ICollection<Item> Items { get; set; } = new List<Item>();
        public virtual ICollection<InventoryTag> Tags { get; set; } = new List<InventoryTag>();
        public virtual ICollection<InventoryAccess> AccessList { get; set; } = new List<InventoryAccess>();
        public virtual ICollection<InventoryAttachment> Attachments { get; set; } = new List<InventoryAttachment>();
    }
}
