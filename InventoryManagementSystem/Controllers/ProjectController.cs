using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Data.Dtos;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProjectController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Project
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetProjects()
        {
            var projects = await _context.Projects
                .Include(p => p.Supervisor)
                .Include(p => p.Products)
                .Select(p => new
                {
                    p.ProjectId,
                    p.Name,
                    p.Code,
                    p.Location,
                    p.Budget,
                    p.Status,
                    p.CreatedAt,
                    p.SupervisorId,
                    SupervisorName = p.Supervisor != null ? p.Supervisor.FullName : null,
                    SupervisorEmail = p.Supervisor != null ? p.Supervisor.Email : null,
                    SupervisorPhone = p.Supervisor != null ? p.Supervisor.Phone : null,
                    TotalMaterialsCount = p.Products.Count,
                    TotalInventoryUnits = p.Products.Sum(prod => prod.Quantity),
                    TotalInventoryValuation = p.Products.Sum(prod => prod.Price * prod.Quantity),
                    LowStockAlertsCount = p.Products.Count(prod => prod.Quantity <= 10)
                })
                .ToListAsync();

            return Ok(projects);
        }

        // GET: api/Project/unassigned - projects that do not currently have a supervisor
        [HttpGet("unassigned")]
        public async Task<ActionResult<IEnumerable<object>>> GetUnassignedProjects()
        {
            // Find projects where no user has AssignedProjectId == project.ProjectId
            var assignedProjectIds = await _context.Users
                .Where(u => u.AssignedProjectId.HasValue)
                .Select(u => u.AssignedProjectId.Value)
                .ToListAsync();

            var unassignedProjects = await _context.Projects
                .Where(p => !assignedProjectIds.Contains(p.ProjectId))
                .Select(p => new
                {
                    p.ProjectId,
                    p.Name,
                    p.Code,
                    p.Location,
                    p.Status
                })
                .ToListAsync();

            return Ok(unassignedProjects);
        }

        // GET: api/Project/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetProject(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Supervisor)
                .Include(p => p.Categories)
                .Include(p => p.Suppliers)
                .Include(p => p.Products)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project == null)
            {
                return NotFound(new { message = $"Project with ID {id} not found." });
            }

            var result = new
            {
                project.ProjectId,
                project.Name,
                project.Code,
                project.Location,
                project.Budget,
                project.Status,
                project.CreatedAt,
                project.SupervisorId,
                Supervisor = project.Supervisor != null ? new
                {
                    project.Supervisor.UserId,
                    project.Supervisor.FullName,
                    project.Supervisor.Email,
                    project.Supervisor.Phone
                } : null,
                Categories = project.Categories.Select(c => new { c.CategoryId, c.Name, ProductsCount = project.Products.Count(p => p.CategoryId == c.CategoryId) }),
                Suppliers = project.Suppliers.Select(s => new { s.Id, s.Name, s.Phone, s.Address, ProductsCount = project.Products.Count(p => p.SupplierId == s.Id) }),
                Products = project.Products.Select(p => new
                {
                    p.ProductId,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.Quantity,
                    p.Unit,
                    p.ReceiptSlipUrl,
                    p.CategoryId,
                    CategoryName = project.Categories.FirstOrDefault(c => c.CategoryId == p.CategoryId)?.Name ?? "General",
                    p.SupplierId,
                    SupplierName = project.Suppliers.FirstOrDefault(s => s.Id == p.SupplierId)?.Name ?? "Unknown",
                    p.CreatedAt
                }),
                TotalMaterialsCount = project.Products.Count,
                TotalInventoryUnits = project.Products.Sum(p => p.Quantity),
                TotalInventoryValuation = project.Products.Sum(p => p.Price * p.Quantity),
                LowStockCount = project.Products.Count(p => p.Quantity <= 10)
            };

            return Ok(result);
        }

        // POST: api/Project
        [HttpPost]
        public async Task<ActionResult<Project>> PostProject([FromBody] ProjectDto projectDto)
        {
            if (string.IsNullOrWhiteSpace(projectDto.Name))
            {
                return BadRequest(new { message = "Project Name is required." });
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
                        command.CommandText = "SELECT COLUMNPROPERTY(OBJECT_ID('Projects'), 'ProjectId', 'IsIdentity')";
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
            if (!isIdentity)
            {
                var maxId = await _context.Projects.AnyAsync() ? await _context.Projects.MaxAsync(p => p.ProjectId) : 0;
                newId = maxId + 1;
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO Projects (ProjectId, Name, Code, Location, Budget, Status, CreatedAt) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})",
                    newId, projectDto.Name.Trim(), projectDto.Code ?? $"PRJ-{newId}", projectDto.Location ?? string.Empty, projectDto.Budget, projectDto.Status ?? "Active", DateTime.UtcNow);

                var createdProject = await _context.Projects.FindAsync(newId);
                return CreatedAtAction(nameof(GetProject), new { id = newId }, createdProject);
            }
            else
            {
                var project = new Project
                {
                    Name = projectDto.Name.Trim(),
                    Code = string.IsNullOrWhiteSpace(projectDto.Code) ? $"PRJ-{DateTime.UtcNow.Ticks % 1000}" : projectDto.Code.Trim(),
                    Location = projectDto.Location ?? string.Empty,
                    Budget = projectDto.Budget,
                    Status = string.IsNullOrWhiteSpace(projectDto.Status) ? "Active" : projectDto.Status.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetProject), new { id = project.ProjectId }, project);
            }
        }

        // PUT: api/Project/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProject(int id, [FromBody] ProjectDto projectDto)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound(new { message = $"Project with ID {id} not found." });
            }

            project.Name = projectDto.Name.Trim();
            if (!string.IsNullOrWhiteSpace(projectDto.Code)) project.Code = projectDto.Code.Trim();
            project.Location = projectDto.Location ?? string.Empty;
            project.Budget = projectDto.Budget;
            project.Status = projectDto.Status ?? "Active";

            await _context.SaveChangesAsync();
            return Ok(project);
        }

        // DELETE: api/Project/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Supervisor)
                .Include(p => p.Categories)
                .Include(p => p.Suppliers)
                .Include(p => p.Products)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project == null)
            {
                return NotFound(new { message = $"Project with ID {id} not found." });
            }

            // Unassign supervisor if assigned
            if (project.Supervisor != null)
            {
                project.Supervisor.AssignedProjectId = null;
            }

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Project '{project.Name}' and its inventory deleted successfully." });
        }
    }
}
