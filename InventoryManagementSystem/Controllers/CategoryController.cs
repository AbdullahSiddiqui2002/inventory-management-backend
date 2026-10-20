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
    public class CategoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Category
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories([FromQuery] int? projectId = null)
        {
            var query = _context.Categories.Include(c => c.Products).AsQueryable();
            if (projectId.HasValue && projectId.Value > 0)
            {
                query = query.Where(c => c.ProjectId == projectId.Value);
            }
            return await query.ToListAsync();
        }

        // GET: api/Category/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories.Include(c => c.Products)
                                                    .FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null)
            {
                return NotFound();
            }
            return category;
        }

        // POST: api/Category
        [HttpPost]
        public async Task<ActionResult<Category>> PostCategory(Category category)
        {
            var isIdentity = true;
            try
            {
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "SELECT COLUMNPROPERTY(OBJECT_ID('Categories'), 'CategoryId', 'IsIdentity')";
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
                var maxId = await _context.Categories.AnyAsync() ? await _context.Categories.MaxAsync(c => c.CategoryId) : 0;
                category.CategoryId = maxId + 1;
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO Categories (CategoryId, Name, ProjectId) VALUES ({0}, {1}, {2})",
                    category.CategoryId, category.Name ?? string.Empty, (object?)category.ProjectId ?? DBNull.Value);
                return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, category);
            }

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, category);
        }

        // POST: api/Category/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult> PostCategoriesBulk(List<Category> categories)
        {
            if (categories == null || categories.Count == 0)
            {
                return BadRequest("The category list cannot be empty.");
            }

            var isIdentity = true;
            try
            {
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "SELECT COLUMNPROPERTY(OBJECT_ID('Categories'), 'CategoryId', 'IsIdentity')";
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
                var nextId = await _context.Categories.AnyAsync() ? await _context.Categories.MaxAsync(c => c.CategoryId) : 0;
                foreach (var cat in categories)
                {
                    nextId++;
                    cat.CategoryId = nextId;
                    await _context.Database.ExecuteSqlRawAsync(
                        "INSERT INTO Categories (CategoryId, Name, ProjectId) VALUES ({0}, {1}, {2})",
                        cat.CategoryId, cat.Name ?? string.Empty, (object?)cat.ProjectId ?? DBNull.Value);
                }
                return Ok(new { message = $"{categories.Count} categories added successfully." });
            }

            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"{categories.Count} categories added successfully." });
        }

        // PUT: api/Category/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCategory(int id, Category category)
        {
            if (id != category.CategoryId)
            {
                return BadRequest();
            }

            _context.Entry(category).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id))
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

        // DELETE: api/Category/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }
    }
}
