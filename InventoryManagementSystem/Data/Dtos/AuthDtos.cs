namespace InventoryManagementSystem.Data.Dtos
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Admin" | "Supervisor"
        public int? AssignedProjectId { get; set; }
        public string? AssignedProjectName { get; set; }
        public string Token { get; set; } = string.Empty;
    }
}
