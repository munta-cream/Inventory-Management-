using System.Collections.Generic;

namespace Inventory_Management_Requirements.Models
{
    public class InventoryDetailsViewModel
    {
        public Inventory Inventory { get; set; }
        public List<Comment> Comments { get; set; }
        public List<InventoryAccess> AccessList { get; set; }
        public List<InventoryAttachment> Attachments { get; set; }
    }
}
