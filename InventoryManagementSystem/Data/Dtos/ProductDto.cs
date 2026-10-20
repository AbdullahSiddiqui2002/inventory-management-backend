namespace InventoryManagementSystem.Data.Dtos
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }
        public int Quantity { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Unit { get; set; } = "Units";
        public string ReceiptSlipUrl { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public int SupplierId { get; set; }
        public int? ProjectId { get; set; }
    }
}
