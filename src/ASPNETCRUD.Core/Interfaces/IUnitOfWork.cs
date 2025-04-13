namespace ASPNETCRUD.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IProductRepository Products { get; }
        ICategoryRepository Categories { get; }
        IUserRepository Users { get; }
        
        Task<int> CompleteAsync();
    }
} 