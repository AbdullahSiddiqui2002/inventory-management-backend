using Microsoft.AspNetCore.Mvc;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System;

namespace InventoryManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SupplierController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SupplierController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Supplier
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers([FromQuery] int? projectId = null)
        {
            var query = _context.Suppliers.Include(s => s.Products).AsQueryable();
            if (projectId.HasValue && projectId.Value > 0)
            {
                query = query.Where(s => s.ProjectId == projectId.Value);
            }
            return await query.ToListAsync();
        }

        // GET: api/Supplier/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Supplier>> GetSupplier(int id)
        {
            var supplier = await _context.Suppliers.Include(s => s.Products)
                                                   .FirstOrDefaultAsync(s => s.Id == id);

            if (supplier == null)
            {
                return NotFound();
            }

            return supplier;
        }

        // POST: api/Supplier
        [HttpPost]
        public async Task<ActionResult<Supplier>> PostSupplier(Supplier supplier)
        {
            var isIdentity = true;
            try
            {
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "SELECT COLUMNPROPERTY(OBJECT_ID('Suppliers'), 'Id', 'IsIdentity')";
                if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    await _context.Database.OpenConnectionAsync();
                var res = await cmd.ExecuteScalarAsync();
                if (res != null && Convert.ToInt32(res) == 0)
                {
                    isIdentity = false;
                }
            }
            catch { }

            if (!isIdentity)
            {
                var maxId = await _context.Suppliers.AnyAsync() ? await _context.Suppliers.MaxAsync(s => s.Id) : 0;
                supplier.Id = maxId + 1;
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO Suppliers (Id, Name, Address, Phone, ProjectId) VALUES ({0}, {1}, {2}, {3}, {4})",
                    supplier.Id, supplier.Name ?? string.Empty, supplier.Address ?? string.Empty, supplier.Phone ?? string.Empty, (object?)supplier.ProjectId ?? DBNull.Value);
                return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, supplier);
            }

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, supplier);
        }

        // POST: api/Supplier/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult> PostSuppliersBulk(List<Supplier> suppliers)
        {
            if (suppliers == null || suppliers.Count == 0)
            {
                return BadRequest("The supplier list cannot be empty.");
            }

            var isIdentity = true;
            try
            {
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "SELECT COLUMNPROPERTY(OBJECT_ID('Suppliers'), 'Id', 'IsIdentity')";
                if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    await _context.Database.OpenConnectionAsync();
                var res = await cmd.ExecuteScalarAsync();
                if (res != null && Convert.ToInt32(res) == 0)
                {
                    isIdentity = false;
                }
            }
            catch { }

            if (!isIdentity)
            {
                var nextId = await _context.Suppliers.AnyAsync() ? await _context.Suppliers.MaxAsync(s => s.Id) : 0;
                foreach (var sup in suppliers)
                {
                    nextId++;
                    sup.Id = nextId;
                    await _context.Database.ExecuteSqlRawAsync(
                        "INSERT INTO Suppliers (Id, Name, Address, Phone, ProjectId) VALUES ({0}, {1}, {2}, {3}, {4})",
                        sup.Id, sup.Name ?? string.Empty, sup.Address ?? string.Empty, sup.Phone ?? string.Empty, (object?)sup.ProjectId ?? DBNull.Value);
                }
                return Ok(new { message = $"{suppliers.Count} suppliers added successfully." });
            }

            _context.Suppliers.AddRange(suppliers);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"{suppliers.Count} suppliers added successfully." });
        }

        // PUT: api/Supplier/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSupplier(int id, Supplier supplier)
        {
            if (id != supplier.Id)
            {
                return BadRequest();
            }

            _context.Entry(supplier).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SupplierExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Supplier/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SupplierExists(int id)
        {
            return _context.Suppliers.Any(e => e.Id == id);
        }
    }
}
