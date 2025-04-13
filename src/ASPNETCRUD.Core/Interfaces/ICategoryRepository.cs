using ASPNETCRUD.Core.Entities;

namespace ASPNETCRUD.Core.Interfaces
{
    public interface ICategoryRepository : IRepositoryBase<Category>
    {
        Task<IReadOnlyList<Category>> GetCategoriesWithProductsAsync();
    }
} 