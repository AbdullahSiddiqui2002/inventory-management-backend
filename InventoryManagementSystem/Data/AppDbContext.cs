using Microsoft.EntityFrameworkCore;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Data
{
    public class AppDbContext : DbContext
    {
        private static bool _schemaInitialized = false;
        private static readonly object _lock = new object();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            if (!_schemaInitialized)
            {
                lock (_lock)
                {
                    if (!_schemaInitialized)
                    {
                        try
                        {
                            Seeding.EnsureSchemaAsync(this).GetAwaiter().GetResult();
                        }
                        catch { }
                        _schemaInitialized = true;
                    }
                }
            }
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1-to-1 relationship between Project and Supervisor (User)
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Supervisor)
                .WithOne(u => u.AssignedProject)
                .HasForeignKey<User>(u => u.AssignedProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            // One-to-many relationship between Project and Categories
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Categories)
                .WithOne(c => c.Project)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many relationship between Project and Suppliers
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Suppliers)
                .WithOne(s => s.Project)
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many relationship between Project and Products
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Products)
                .WithOne(prod => prod.Project)
                .HasForeignKey(prod => prod.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuring one-to-many relationship between Supplier and Product
            modelBuilder.Entity<Supplier>()
                .HasMany(s => s.Products)
                .WithOne(p => p.Supplier)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuring one-to-many relationship between Category and Product
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Include compile-time seed data
            Seeding.SeedDatabase(modelBuilder);
        }
    }
}