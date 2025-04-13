using ASPNETCRUD.Core.Entities;
using ASPNETCRUD.Core.Interfaces;
using ASPNETCRUD.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ASPNETCRUD.Infrastructure.Repositories
{
    public class CategoryRepository : RepositoryBase<Category>, ICategoryRepository
    {
        public CategoryRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IReadOnlyList<Category>> GetCategoriesWithProductsAsync()
        {
            return await _dbContext.Categories
                .Include(c => c.Products)
                .ToListAsync();
        }
    }
} 