namespace InventoryManagementSystem.Data.Dtos
{
    public class ProjectDto
    {
        public int ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public double Budget { get; set; }
        public string Status { get; set; } = "Active";
        public int? SupervisorId { get; set; }
    }
}
