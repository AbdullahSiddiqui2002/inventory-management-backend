using System.Collections.Generic;

namespace InventoryManagementSystem.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string? Name { get; set; } = string.Empty;

        // Foreign key to Project
        public int? ProjectId { get; set; }
        public Project? Project { get; set; }

        public List<Product> Products { get; set; } = new List<Product>();
    }
}