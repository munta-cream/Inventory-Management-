using System.ComponentModel.DataAnnotations;

namespace Inventory_Management_Requirements.Models
{
    public class InventoryCreateViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        public int? CategoryId { get; set; }

        [StringLength(50, ErrorMessage = "Category name cannot exceed 50 characters")]
        public string? CustomCategoryName { get; set; }

        public bool IsCustomCategory => CategoryId == -1;

        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPublic { get; set; }

        // Item fields
        public string? ItemName { get; set; }
        public string? ItemDescription { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal? Weight { get; set; }
        public bool InStock { get; set; }
        public bool Fragile { get; set; }
        public bool Perishable { get; set; }
    }
}
