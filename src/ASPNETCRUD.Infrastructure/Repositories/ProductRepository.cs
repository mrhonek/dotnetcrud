using ASPNETCRUD.Core.Entities;
using ASPNETCRUD.Core.Interfaces;
using ASPNETCRUD.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ASPNETCRUD.Infrastructure.Repositories
{
    public class ProductRepository : RepositoryBase<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _dbContext.Products
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Product>> GetProductsWithCategoriesAsync()
        {
            return await _dbContext.Products
                .Include(p => p.Category)
                .ToListAsync();
        }
    }
} 