using System;

namespace InventoryManagementSystem.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string? Username { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;
        public string? Password { get; set; } = string.Empty;
        public string? FullName { get; set; } = string.Empty;
        public string? Phone { get; set; } = string.Empty;
        public string? Role { get; set; } = "Supervisor"; // "Admin" | "Supervisor"

        // Assigned project for supervisor (1-to-1)
        public int? AssignedProjectId { get; set; }
        public Project? AssignedProject { get; set; }

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
