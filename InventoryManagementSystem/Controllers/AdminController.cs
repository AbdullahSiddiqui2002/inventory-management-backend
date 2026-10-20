using Microsoft.AspNetCore.Mvc;
using InventoryManagementSystem.Data;
using System.Threading.Tasks;
using System;

namespace InventoryManagementSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Admin/reset-pakistan-data
        [HttpPost("reset-pakistan-data")]
        public async Task<IActionResult> ResetPakistanConstructionData()
        {
            try
            {
                await Seeding.ResetToPakistaniConstructionDataAsync(_context);
                return Ok(new
                {
                    message = "Successfully cleaned all old data and loaded authentic Pakistani Construction Projects, Categories, Suppliers, and Materials!",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to reset data", details = ex.Message });
            }
        }
    }
}
