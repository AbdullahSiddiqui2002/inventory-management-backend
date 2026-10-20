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
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email and Password are required." });
            }

            var user = await _context.Users
                .Include(u => u.AssignedProject)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.Trim().ToLower());

            if (user == null || user.Password != request.Password.Trim())
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var response = new AuthResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                AssignedProjectId = user.AssignedProjectId,
                AssignedProjectName = user.AssignedProject?.Name,
                Token = $"mock-jwt-token-{user.UserId}-{user.Role.ToLower()}"
            };

            return Ok(response);
        }

        // GET: api/Auth/me/{userId}
        [HttpGet("me/{userId}")]
        public async Task<ActionResult<AuthResponse>> GetProfile(int userId)
        {
            var user = await _context.Users
                .Include(u => u.AssignedProject)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var response = new AuthResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                AssignedProjectId = user.AssignedProjectId,
                AssignedProjectName = user.AssignedProject?.Name,
                Token = $"mock-jwt-token-{user.UserId}-{user.Role.ToLower()}"
            };

            return Ok(response);
        }

        // GET: api/Auth/users - list all users (for Admin demo selector)
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            var users = await _context.Users
                .Include(u => u.AssignedProject)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.Email,
                    u.FullName,
                    u.Role,
                    u.Phone,
                    u.AssignedProjectId,
                    AssignedProjectName = u.AssignedProject != null ? u.AssignedProject.Name : null
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}
