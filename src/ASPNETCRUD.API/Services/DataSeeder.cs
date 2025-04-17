using ASPNETCRUD.Core.Entities;
using ASPNETCRUD.Infrastructure.Data;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace ASPNETCRUD.API.Services
{
    public class DataSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DataSeeder> _logger;

        public DataSeeder(ApplicationDbContext context, ILogger<DataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedDemoDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting database seeding");
                
                // Clear existing data - we use a transaction to ensure all-or-nothing
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    await ClearAllDataAsync();
                    
                    // Add sample users
                    await SeedUsersAsync();
                    
                    // Add sample categories
                    await SeedCategoriesAsync();
                    
                    // Add sample products
                    await SeedProductsAsync();
                    
                    // Commit transaction
                    await transaction.CommitAsync();
                    _logger.LogInformation("Database seeding completed successfully");
                }
                catch (Exception ex)
                {
                    // Roll back transaction if any operation fails
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error during database seeding, transaction rolled back");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database seeding");
                throw;
            }
        }
        
        private async Task ClearAllDataAsync()
        {
            // Delete data in the correct order to respect foreign keys
            _logger.LogInformation("Clearing existing data");
            
            // Reset identity columns
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE \"Products\" RESTART IDENTITY CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE \"Categories\" RESTART IDENTITY CASCADE");
            await _context.Database.ExecuteSqlRawAsync("TRUNCATE \"Users\" RESTART IDENTITY CASCADE");
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("All existing data cleared");
        }
        
        private async Task SeedUsersAsync()
        {
            _logger.LogInformation("Seeding user data");
            
            // Add demo user
            var demoUser = new User
            {
                Username = "demo",
                Email = "demo@example.com",
                FirstName = "Demo",
                LastName = "User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Demo@123"),
                Roles = new List<string> { "User", "Admin" },
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.Users.AddAsync(demoUser);
            
            // Add regular user
            var regularUser = new User
            {
                Username = "user",
                Email = "user@example.com",
                FirstName = "Regular",
                LastName = "User",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
                Roles = new List<string> { "User" },
                CreatedAt = DateTime.UtcNow
            };
            
            await _context.Users.AddAsync(regularUser);
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("User data seeding completed");
        }
        
        private async Task SeedCategoriesAsync()
        {
            _logger.LogInformation("Seeding categories");
            
            var categories = new List<Category>
            {
                new Category { Name = "Electronics", Description = "Electronic devices and accessories", CreatedAt = DateTime.UtcNow },
                new Category { Name = "Clothing", Description = "Fashion items and accessories", CreatedAt = DateTime.UtcNow },
                new Category { Name = "Books", Description = "Books, e-books, and publications", CreatedAt = DateTime.UtcNow },
                new Category { Name = "Home & Kitchen", Description = "Household and kitchen items", CreatedAt = DateTime.UtcNow },
                new Category { Name = "Sports", Description = "Sports equipment and outdoor gear", CreatedAt = DateTime.UtcNow }
            };
            
            await _context.Categories.AddRangeAsync(categories);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Categories seeding completed");
        }
        
        private async Task SeedProductsAsync()
        {
            _logger.LogInformation("Seeding products");
            
            // Get category IDs
            var categories = await _context.Categories.ToListAsync();
            
            var electronicsId = categories.First(c => c.Name == "Electronics").Id;
            var clothingId = categories.First(c => c.Name == "Clothing").Id;
            var booksId = categories.First(c => c.Name == "Books").Id;
            var homeKitchenId = categories.First(c => c.Name == "Home & Kitchen").Id;
            var sportsId = categories.First(c => c.Name == "Sports").Id;
            
            var products = new List<Product>
            {
                // Electronics
                new Product { 
                    Name = "Smartphone", 
                    Description = "Latest smartphone with high-resolution camera", 
                    Price = 899.99m, 
                    Stock = 50, 
                    CategoryId = electronicsId, 
                    CreatedAt = DateTime.UtcNow 
                },
                new Product { 
                    Name = "Laptop", 
                    Description = "Powerful laptop for productivity and gaming", 
                    Price = 1299.99m, 
                    Stock = 30, 
                    CategoryId = electronicsId, 
                    CreatedAt = DateTime.UtcNow 
                },
                new Product { 
                    Name = "Wireless Headphones", 
                    Description = "Noise-cancelling wireless headphones", 
                    Price = 199.99m, 
                    Stock = 100, 
                    CategoryId = electronicsId, 
                    CreatedAt = DateTime.UtcNow 
                },
                
                // Clothing
                new Product { 
                    Name = "T-Shirt", 
                    Description = "Comfortable cotton t-shirt", 
                    Price = 19.99m, 
                    Stock = 200, 
                    CategoryId = clothingId, 
                    CreatedAt = DateTime.UtcNow 
                },
                new Product { 
                    Name = "Jeans", 
                    Description = "Durable denim jeans", 
                    Price = 49.99m, 
                    Stock = 150, 
                    CategoryId = clothingId, 
                    CreatedAt = DateTime.UtcNow 
                },
                
                // Books
                new Product { 
                    Name = "Programming Guide", 
                    Description = "Comprehensive programming guide for beginners", 
                    Price = 29.99m, 
                    Stock = 75, 
                    CategoryId = booksId, 
                    CreatedAt = DateTime.UtcNow 
                },
                new Product { 
                    Name = "Science Fiction Novel", 
                    Description = "Bestselling science fiction novel", 
                    Price = 14.99m, 
                    Stock = 120, 
                    CategoryId = booksId, 
                    CreatedAt = DateTime.UtcNow 
                },
                
                // Home & Kitchen
                new Product { 
                    Name = "Coffee Maker", 
                    Description = "Automatic coffee maker with timer", 
                    Price = 89.99m, 
                    Stock = 40, 
                    CategoryId = homeKitchenId, 
                    CreatedAt = DateTime.UtcNow 
                },
                new Product { 
                    Name = "Cookware Set", 
                    Description = "Non-stick cookware set, 10 pieces", 
                    Price = 149.99m, 
                    Stock = 25, 
                    CategoryId = homeKitchenId, 
                    CreatedAt = DateTime.UtcNow 
                },
                
                // Sports
                new Product { 
                    Name = "Yoga Mat", 
                    Description = "Non-slip yoga mat", 
                    Price = 24.99m, 
                    Stock = 80, 
                    CategoryId = sportsId, 
                    CreatedAt = DateTime.UtcNow 
                },
                new Product { 
                    Name = "Fitness Tracker", 
                    Description = "Digital fitness tracker with heart rate monitor", 
                    Price = 79.99m, 
                    Stock = 60, 
                    CategoryId = sportsId, 
                    CreatedAt = DateTime.UtcNow 
                }
            };
            
            await _context.Products.AddRangeAsync(products);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Products seeding completed");
        }
    }
} 