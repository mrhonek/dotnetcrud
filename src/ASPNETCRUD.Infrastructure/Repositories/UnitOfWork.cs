using ASPNETCRUD.Core.Interfaces;
using ASPNETCRUD.Infrastructure.Data;

namespace ASPNETCRUD.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;
        private IProductRepository _productRepository;
        private ICategoryRepository _categoryRepository;
        private IUserRepository _userRepository;
        private bool _disposed;

        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _productRepository = new ProductRepository(_dbContext);
            _categoryRepository = new CategoryRepository(_dbContext);
            _userRepository = new UserRepository(_dbContext);
        }

        public IProductRepository Products => _productRepository;

        public ICategoryRepository Categories => _categoryRepository;

        public IUserRepository Users => _userRepository;

        public async Task<int> CompleteAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }

        public object GetDbContext()
        {
            return _dbContext;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _dbContext.Dispose();
                }
                _disposed = true;
            }
        }
    }
} 