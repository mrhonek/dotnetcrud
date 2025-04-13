using ASPNETCRUD.Core.Entities;

namespace ASPNETCRUD.Core.Interfaces
{
    public interface IProductRepository : IRepositoryBase<Product>
    {
        Task<IReadOnlyList<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<IReadOnlyList<Product>> GetProductsWithCategoriesAsync();
    }
} 