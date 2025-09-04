using Microsoft.AspNetCore.Identity;

namespace Inventory_Management_Requirements.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsAdmin { get; set; } = false;
        public bool IsBlocked { get; set; } = false;
    }
}
