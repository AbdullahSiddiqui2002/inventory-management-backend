using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagementSystem.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string? Name { get; set; } = string.Empty;
        public double Price { get; set; }
        public int Quantity { get; set; }
        public string? Description { get; set; } = string.Empty;
        public string? Unit { get; set; } = "Units";
        public string? ReceiptSlipUrl { get; set; } = string.Empty;

        // Foreign key to Category
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [NotMapped]
        public string? CategoryName { get; set; } = string.Empty;

        // Foreign key to Supplier
        public int SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
        
        [NotMapped]
        public string? SupplierName { get; set; } = string.Empty;

        // Foreign key to Project
        public int? ProjectId { get; set; }
        public Project? Project { get; set; }

        [NotMapped]
        public string? ProjectName { get; set; } = string.Empty;

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
