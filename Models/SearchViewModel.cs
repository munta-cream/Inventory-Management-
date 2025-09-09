using System.Collections.Generic;
using Inventory_Management_Requirements.Models;

namespace Inventory_Management_Requirements.Models
{
    public class SearchViewModel
    {
        public string SearchTerm { get; set; }
        public List<Inventory> Inventories { get; set; }
        public List<Item> Items { get; set; }
    }
}
