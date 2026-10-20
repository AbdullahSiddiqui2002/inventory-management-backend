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
    public class SupervisorController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SupervisorController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Supervisor
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetSupervisors()
        {
            var supervisors = await _context.Users
                .Where(u => u.Role == "Supervisor")
                .Include(u => u.AssignedProject)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.Email,
                    u.FullName,
                    u.Phone,
                    u.Role,
                    u.AssignedProjectId,
                    AssignedProjectName = u.AssignedProject != null ? u.AssignedProject.Name : null,
                    AssignedProjectCode = u.AssignedProject != null ? u.AssignedProject.Code : null,
                    AssignedProjectLocation = u.AssignedProject != null ? u.AssignedProject.Location : null,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(supervisors);
        }

        // GET: api/Supervisor/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetSupervisor(int id)
        {
            var supervisor = await _context.Users
                .Include(u => u.AssignedProject)
                .FirstOrDefaultAsync(u => u.UserId == id && u.Role == "Supervisor");

            if (supervisor == null)
            {
                return NotFound(new { message = $"Supervisor with ID {id} not found." });
            }

            return Ok(new
            {
                supervisor.UserId,
                supervisor.Username,
                supervisor.Email,
                supervisor.FullName,
                supervisor.Phone,
                supervisor.Role,
                supervisor.AssignedProjectId,
                AssignedProjectName = supervisor.AssignedProject?.Name,
                supervisor.CreatedAt
            });
        }

        // POST: api/Supervisor
        [HttpPost]
        public async Task<ActionResult<object>> PostSupervisor([FromBody] SupervisorDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                return BadRequest(new { message = "Full Name is required." });
            }
            if (string.IsNullOrWhiteSpace(dto.Email))
            {
                return BadRequest(new { message = "Email is required." });
            }

            var existingEmail = await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower());
            if (existingEmail)
            {
                return BadRequest(new { message = "A user with this email already exists." });
            }

            // 1-to-1 Project Constraint Check: Ensure no other user is assigned to this project
            if (dto.AssignedProjectId.HasValue && dto.AssignedProjectId.Value > 0)
            {
                var project = await _context.Projects.FindAsync(dto.AssignedProjectId.Value);
                if (project == null)
                {
                    return BadRequest(new { message = "Selected project does not exist." });
                }

                var alreadyAssigned = await _context.Users
                    .AnyAsync(u => u.AssignedProjectId == dto.AssignedProjectId.Value);

                if (alreadyAssigned)
                {
                    return BadRequest(new { message = "This project already has an assigned supervisor. Each project can only have 1 supervisor." });
                }
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
                        command.CommandText = "SELECT COLUMNPROPERTY(OBJECT_ID('Users'), 'UserId', 'IsIdentity')";
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
            var username = !string.IsNullOrWhiteSpace(dto.Username) ? dto.Username.Trim() : dto.Email.Split('@')[0];
            var password = !string.IsNullOrWhiteSpace(dto.Password) ? dto.Password : "pass123";

            if (!isIdentity)
            {
                var maxId = await _context.Users.AnyAsync() ? await _context.Users.MaxAsync(u => u.UserId) : 0;
                newId = maxId + 1;
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO Users (UserId, Username, Email, Password, FullName, Phone, Role, AssignedProjectId, CreatedAt) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8})",
                    newId, username, dto.Email.Trim(), password, dto.FullName.Trim(), dto.Phone ?? string.Empty, "Supervisor", (object?)dto.AssignedProjectId ?? DBNull.Value, DateTime.UtcNow);
            }
            else
            {
                var user = new User
                {
                    Username = username,
                    Email = dto.Email.Trim(),
                    Password = password,
                    FullName = dto.FullName.Trim(),
                    Phone = dto.Phone ?? string.Empty,
                    Role = "Supervisor",
                    AssignedProjectId = dto.AssignedProjectId > 0 ? dto.AssignedProjectId : null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                newId = user.UserId;
            }

            // Sync Project's SupervisorId
            if (dto.AssignedProjectId.HasValue && dto.AssignedProjectId.Value > 0)
            {
                var prj = await _context.Projects.FindAsync(dto.AssignedProjectId.Value);
                if (prj != null)
                {
                    prj.SupervisorId = newId;
                    await _context.SaveChangesAsync();
                }
            }

            var createdUser = await _context.Users
                .Include(u => u.AssignedProject)
                .FirstOrDefaultAsync(u => u.UserId == newId);

            return CreatedAtAction(nameof(GetSupervisor), new { id = newId }, createdUser);
        }

        // PUT: api/Supervisor/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSupervisor(int id, [FromBody] SupervisorDto dto)
        {
            var supervisor = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id && u.Role == "Supervisor");
            if (supervisor == null)
            {
                return NotFound(new { message = $"Supervisor with ID {id} not found." });
            }

            if (!string.IsNullOrWhiteSpace(dto.FullName)) supervisor.FullName = dto.FullName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Email)) supervisor.Email = dto.Email.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Phone)) supervisor.Phone = dto.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Password)) supervisor.Password = dto.Password;

            // Handle Project Reassignment
            if (dto.AssignedProjectId != supervisor.AssignedProjectId)
            {
                if (dto.AssignedProjectId.HasValue && dto.AssignedProjectId.Value > 0)
                {
                    // Check if other user is assigned to new project
                    var alreadyAssigned = await _context.Users
                        .AnyAsync(u => u.UserId != id && u.AssignedProjectId == dto.AssignedProjectId.Value);

                    if (alreadyAssigned)
                    {
                        return BadRequest(new { message = "The selected project is already assigned to another supervisor." });
                    }

                    // Remove from old project
                    if (supervisor.AssignedProjectId.HasValue)
                    {
                        var oldPrj = await _context.Projects.FindAsync(supervisor.AssignedProjectId.Value);
                        if (oldPrj != null && oldPrj.SupervisorId == id) oldPrj.SupervisorId = null;
                    }

                    // Assign to new project
                    supervisor.AssignedProjectId = dto.AssignedProjectId.Value;
                    var newPrj = await _context.Projects.FindAsync(dto.AssignedProjectId.Value);
                    if (newPrj != null) newPrj.SupervisorId = id;
                }
                else
                {
                    // Unassign
                    if (supervisor.AssignedProjectId.HasValue)
                    {
                        var oldPrj = await _context.Projects.FindAsync(supervisor.AssignedProjectId.Value);
                        if (oldPrj != null && oldPrj.SupervisorId == id) oldPrj.SupervisorId = null;
                    }
                    supervisor.AssignedProjectId = null;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(supervisor);
        }

        // DELETE: api/Supervisor/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupervisor(int id)
        {
            var supervisor = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id && u.Role == "Supervisor");
            if (supervisor == null)
            {
                return NotFound(new { message = $"Supervisor with ID {id} not found." });
            }

            if (supervisor.AssignedProjectId.HasValue)
            {
                var prj = await _context.Projects.FindAsync(supervisor.AssignedProjectId.Value);
                if (prj != null && prj.SupervisorId == id) prj.SupervisorId = null;
            }

            _context.Users.Remove(supervisor);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Supervisor '{supervisor.FullName}' removed successfully." });
        }
    }
}
