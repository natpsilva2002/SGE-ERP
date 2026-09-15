using SGE.Application.DTOs.Category;

namespace SGE.Application.Interfaces.Services.Catalog;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllAsync();

    Task<CategoryDto?> GetByIdAsync(Guid id);

    Task<CategoryDto> CreateAsync(CreateCategoryDto dto);

    Task<CategoryDto?> UpdateAsync(Guid id, UpdateCategoryDto dto);

    Task<bool> DeleteAsync(Guid id);
}