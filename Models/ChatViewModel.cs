using System.Collections.Generic;

namespace Inventory_Management_Requirements.Models
{
    public class ChatViewModel
    {
        public List<Comment> Comments { get; set; }
        public int? AttachmentId { get; set; }
    }
}
