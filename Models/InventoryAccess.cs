namespace Inventory_Management_Requirements.Models
{
    public class InventoryAccess
    {
        public int InventoryId { get; set; }
        public virtual Inventory Inventory { get; set; }
        
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
        
        public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
        public string GrantedById { get; set; }
        public virtual ApplicationUser GrantedBy { get; set; }
        
        public bool CanEdit { get; set; } = true;
        public bool CanManage { get; set; } = false;
    }
}
