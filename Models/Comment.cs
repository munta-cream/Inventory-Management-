using System.ComponentModel.DataAnnotations;

namespace Inventory_Management_Requirements.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public int InventoryId { get; set; }
        public virtual Inventory Inventory { get; set; }
        
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
        
        [Required]
        [StringLength(1000)]
        public string Content { get; set; }
        
        public int? ParentCommentId { get; set; }
        public virtual Comment ParentComment { get; set; }
        public virtual ICollection<Comment> Replies { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
    }
}
