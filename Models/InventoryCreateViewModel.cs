using System.ComponentModel.DataAnnotations;

namespace Inventory_Management_Requirements.Models
{
    public class InventoryCreateViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        public int CategoryId { get; set; }

        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
    }
}
