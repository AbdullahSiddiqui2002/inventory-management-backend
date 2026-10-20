using System.Collections.Generic;

namespace InventoryManagementSystem.Models
{
    public class Supplier
    {
        public int Id { get; set; }
        public string? Name { get; set; } = string.Empty;
        public string? Address { get; set; } = string.Empty;
        public string? Phone { get; set; } = string.Empty;

        // Foreign key to Project
        public int? ProjectId { get; set; }
        public Project? Project { get; set; }

        // Navigation property to list products
        public List<Product> Products { get; set; } = new List<Product>();

        public Supplier() { }

        public Supplier(string? name, string? address, string? phone, int? projectId = null)
        {
            Name = name;
            Address = address;
            Phone = phone;
            ProjectId = projectId;
        }
    }
}
