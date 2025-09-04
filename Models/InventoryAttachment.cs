using System;

namespace Inventory_Management_Requirements.Models
{
    public class InventoryAttachment
    {
        public int Id { get; set; }
        public int InventoryId { get; set; }
        public virtual Inventory Inventory { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string FileType { get; set; } // "image", "pdf", "document", etc.
        public long FileSize { get; set; }
        public string UploadedById { get; set; }
        public virtual ApplicationUser UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
