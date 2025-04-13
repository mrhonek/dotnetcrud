using ASPNETCRUD.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ASPNETCRUD.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Electronics", Description = "Electronic items", CreatedAt = DateTime.UtcNow },
                new Category { Id = 2, Name = "Clothing", Description = "Clothing items", CreatedAt = DateTime.UtcNow },
                new Category { Id = 3, Name = "Books", Description = "Books, novels, and publications", CreatedAt = DateTime.UtcNow }
            );

            // Seed products
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Laptop", Description = "High-performance laptop", Price = 1200.00m, Stock = 10, CategoryId = 1, CreatedAt = DateTime.UtcNow },
                new Product { Id = 2, Name = "Smartphone", Description = "Latest smartphone model", Price = 800.00m, Stock = 15, CategoryId = 1, CreatedAt = DateTime.UtcNow },
                new Product { Id = 3, Name = "T-shirt", Description = "Cotton t-shirt", Price = 20.00m, Stock = 50, CategoryId = 2, CreatedAt = DateTime.UtcNow },
                new Product { Id = 4, Name = "Jeans", Description = "Denim jeans", Price = 50.00m, Stock = 30, CategoryId = 2, CreatedAt = DateTime.UtcNow },
                new Product { Id = 5, Name = "Novel", Description = "Best-selling novel", Price = 15.00m, Stock = 100, CategoryId = 3, CreatedAt = DateTime.UtcNow }
            );
        }
    }
} 