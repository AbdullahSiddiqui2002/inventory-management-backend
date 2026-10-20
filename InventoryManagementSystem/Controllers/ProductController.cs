using Microsoft.AspNetCore.Mvc;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data.Dtos;
using System.Collections.Generic;
using System.Linq;
using System;

namespace InventoryManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Product - Retrieve all products with optional projectId filtering
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetProducts([FromQuery] int? projectId = null)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Project)
                .AsQueryable();

            if (projectId.HasValue && projectId.Value > 0)
            {
                query = query.Where(p => p.ProjectId == projectId.Value);
            }

            var products = await query
                .Select(p => new
                {
                    p.ProductId,
                    Name = p.Name ?? string.Empty,
                    Description = p.Description ?? string.Empty,
                    p.Price,
                    p.Quantity,
                    Unit = p.Unit ?? "Units",
                    ReceiptSlipUrl = p.ReceiptSlipUrl ?? string.Empty,
                    p.CategoryId,
                    CategoryName = p.Category != null ? (p.Category.Name ?? "General") : "General",
                    p.SupplierId,
                    SupplierName = p.Supplier != null ? (p.Supplier.Name ?? "Unknown") : "Unknown",
                    p.ProjectId,
                    ProjectName = p.Project != null ? (p.Project.Name ?? "Unassigned Site") : "Unassigned Site",
                    CreatedAt = p.CreatedAt ?? DateTime.UtcNow
                })
                .ToListAsync();

            return Ok(products);
        }

        // GET: api/Product/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Include(p => p.Project)
                .Select(p => new
                {
                    p.ProductId,
                    Name = p.Name ?? string.Empty,
                    Description = p.Description ?? string.Empty,
                    p.Price,
                    p.Quantity,
                    Unit = p.Unit ?? "Units",
                    ReceiptSlipUrl = p.ReceiptSlipUrl ?? string.Empty,
                    p.CategoryId,
                    CategoryName = p.Category != null ? (p.Category.Name ?? "General") : "General",
                    p.SupplierId,
                    SupplierName = p.Supplier != null ? (p.Supplier.Name ?? "Unknown") : "Unknown",
                    p.ProjectId,
                    ProjectName = p.Project != null ? (p.Project.Name ?? "Unassigned Site") : "Unassigned Site",
                    CreatedAt = p.CreatedAt ?? DateTime.UtcNow
                })
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound(new { message = $"Product with ID {id} not found." });
            }

            return Ok(product);
        }

        // POST: api/Product - Create a new construction material / product
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct([FromBody] ProductDto productDto)
        {
            var category = await _context.Categories.FindAsync(productDto.CategoryId);
            var supplier = await _context.Suppliers.FindAsync(productDto.SupplierId);

            if (category == null)
            {
                return BadRequest(new { message = $"Invalid Category ID: {productDto.CategoryId}." });
            }

            if (supplier == null)
            {
                return BadRequest(new { message = $"Invalid Supplier ID: {productDto.SupplierId}." });
            }

            bool isIdentity = true;
            try
            {
                var connection = _context.Database.GetDbConnection();
                if (_context.Database.IsSqlServer())
                {
                    using (var command = connection.CreateCommand())
                    {
                        await _context.Database.OpenConnectionAsync();
                        command.CommandText = "SELECT COLUMNPROPERTY(OBJECT_ID('Products'), 'ProductId', 'IsIdentity')";
                        var result = await command.ExecuteScalarAsync();
                        if (result == null || Convert.ToInt32(result) == 0)
                        {
                            isIdentity = false;
                        }
                    }
                }
            }
            catch { }

            int newId = 0;
            var unit = !string.IsNullOrWhiteSpace(productDto.Unit) ? productDto.Unit.Trim() : "Units";
            var slipUrl = productDto.ReceiptSlipUrl ?? string.Empty;

            if (!isIdentity)
            {
                var maxId = await _context.Products.AnyAsync() ? await _context.Products.MaxAsync(p => p.ProductId) : 0;
                newId = maxId + 1;
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO Products (ProductId, Name, Description, Price, Quantity, Unit, ReceiptSlipUrl, CategoryId, SupplierId, ProjectId, CreatedAt) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10})",
                    newId, productDto.Name.Trim(), productDto.Description ?? string.Empty, productDto.Price, productDto.Quantity, unit, slipUrl, productDto.CategoryId, productDto.SupplierId, (object?)productDto.ProjectId ?? DBNull.Value, DateTime.UtcNow);

                var createdProduct = await _context.Products.FindAsync(newId);
                return CreatedAtAction(nameof(GetProduct), new { id = newId }, createdProduct);
            }
            else
            {
                var product = new Product
                {
                    Name = productDto.Name.Trim(),
                    Description = productDto.Description ?? string.Empty,
                    Price = productDto.Price,
                    Quantity = productDto.Quantity,
                    Unit = unit,
                    ReceiptSlipUrl = slipUrl,
                    CategoryId = productDto.CategoryId,
                    SupplierId = productDto.SupplierId,
                    ProjectId = productDto.ProjectId > 0 ? productDto.ProjectId : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
            }
        }

        // PUT: api/Product/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, [FromBody] ProductDto productDto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { message = $"Product with ID {id} not found." });
            }

            product.Name = productDto.Name.Trim();
            product.Description = productDto.Description ?? string.Empty;
            product.Price = productDto.Price;
            product.Quantity = productDto.Quantity;
            if (!string.IsNullOrWhiteSpace(productDto.Unit)) product.Unit = productDto.Unit.Trim();
            if (productDto.ReceiptSlipUrl != null) product.ReceiptSlipUrl = productDto.ReceiptSlipUrl;
            product.CategoryId = productDto.CategoryId;
            product.SupplierId = productDto.SupplierId;
            if (productDto.ProjectId.HasValue) product.ProjectId = productDto.ProjectId.Value > 0 ? productDto.ProjectId : null;

            await _context.SaveChangesAsync();
            return Ok(product);
        }

        // DELETE: api/Product/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { message = $"Product with ID {id} not found." });
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Product '{product.Name}' deleted successfully." });
        }

        // POST: api/Product/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult<IEnumerable<Product>>> PostProductsInBulk([FromBody] IEnumerable<ProductDto> productDtos)
        {
            if (productDtos == null || !productDtos.Any())
            {
                return BadRequest(new { message = "No products provided in request." });
            }

            var createdList = new List<Product>();
            foreach (var dto in productDtos)
            {
                var product = new Product
                {
                    Name = dto.Name,
                    Description = dto.Description ?? string.Empty,
                    Price = dto.Price,
                    Quantity = dto.Quantity,
                    Unit = !string.IsNullOrWhiteSpace(dto.Unit) ? dto.Unit : "Units",
                    ReceiptSlipUrl = dto.ReceiptSlipUrl ?? string.Empty,
                    CategoryId = dto.CategoryId,
                    SupplierId = dto.SupplierId,
                    ProjectId = dto.ProjectId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Products.Add(product);
                createdList.Add(product);
            }

            await _context.SaveChangesAsync();
            return Ok(createdList);
        }
    }
}
