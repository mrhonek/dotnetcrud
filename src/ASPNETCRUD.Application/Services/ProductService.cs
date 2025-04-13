using AutoMapper;
using ASPNETCRUD.Application.DTOs;
using ASPNETCRUD.Application.Interfaces;
using ASPNETCRUD.Core.Entities;
using ASPNETCRUD.Core.Exceptions;
using ASPNETCRUD.Core.Interfaces;

namespace ASPNETCRUD.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ProductService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ProductDto> CreateProductAsync(CreateProductDto productDto)
        {
            // Verify category exists
            var categoryExists = await _unitOfWork.Categories.ExistsAsync(productDto.CategoryId);
            if (!categoryExists)
            {
                throw new BadRequestException($"Category with id {productDto.CategoryId} does not exist");
            }

            var product = _mapper.Map<Product>(productDto);
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<ProductDto>(product);
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
            {
                throw new NotFoundException(nameof(Product), id);
            }

            await _unitOfWork.Products.DeleteAsync(product);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            var products = await _unitOfWork.Products.GetProductsWithCategoriesAsync();
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        public async Task<ProductDto> GetProductByIdAsync(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
            {
                throw new NotFoundException(nameof(Product), id);
            }

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(int categoryId)
        {
            var products = await _unitOfWork.Products.GetProductsByCategoryAsync(categoryId);
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }

        public async Task UpdateProductAsync(UpdateProductDto productDto)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productDto.Id);
            if (product == null)
            {
                throw new NotFoundException(nameof(Product), productDto.Id);
            }

            // Verify category exists
            var categoryExists = await _unitOfWork.Categories.ExistsAsync(productDto.CategoryId);
            if (!categoryExists)
            {
                throw new BadRequestException($"Category with id {productDto.CategoryId} does not exist");
            }

            _mapper.Map(productDto, product);
            await _unitOfWork.Products.UpdateAsync(product);
            await _unitOfWork.CompleteAsync();
        }
    }
} 