using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.Models
{
    public class Project
    {
        public int ProjectId { get; set; }
        public string? Name { get; set; } = string.Empty;
        public string? Code { get; set; } = string.Empty;
        public string? Location { get; set; } = string.Empty;
        public double Budget { get; set; }
        public string? Status { get; set; } = "Active";
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        // 1-to-1 relationship with assigned supervisor (User)
        public int? SupervisorId { get; set; }
        public User? Supervisor { get; set; }

        // Navigation collections for isolated inventory
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Supplier> Suppliers { get; set; } = new List<Supplier>();
        public List<Product> Products { get; set; } = new List<Product>();
    }
}
