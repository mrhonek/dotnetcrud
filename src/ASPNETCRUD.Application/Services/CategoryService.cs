using AutoMapper;
using ASPNETCRUD.Application.DTOs;
using ASPNETCRUD.Application.Interfaces;
using ASPNETCRUD.Core.Entities;
using ASPNETCRUD.Core.Exceptions;
using ASPNETCRUD.Core.Interfaces;

namespace ASPNETCRUD.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CategoryService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto categoryDto)
        {
            var category = _mapper.Map<Category>(categoryDto);
            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<CategoryDto>(category);
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null)
            {
                throw new NotFoundException(nameof(Category), id);
            }

            // Check if category has products
            var products = await _unitOfWork.Products.GetProductsByCategoryAsync(id);
            if (products.Count > 0)
            {
                throw new BadRequestException($"Cannot delete category with ID {id} because it has associated products");
            }

            await _unitOfWork.Categories.DeleteAsync(category);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _unitOfWork.Categories.GetAllAsync();
            return _mapper.Map<IEnumerable<CategoryDto>>(categories);
        }

        public async Task<CategoryDto> GetCategoryByIdAsync(int id)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null)
            {
                throw new NotFoundException(nameof(Category), id);
            }

            return _mapper.Map<CategoryDto>(category);
        }

        public async Task UpdateCategoryAsync(UpdateCategoryDto categoryDto)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryDto.Id);
            if (category == null)
            {
                throw new NotFoundException(nameof(Category), categoryDto.Id);
            }

            _mapper.Map(categoryDto, category);
            await _unitOfWork.Categories.UpdateAsync(category);
            await _unitOfWork.CompleteAsync();
        }
    }
} 