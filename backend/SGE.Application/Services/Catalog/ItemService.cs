using SGE.Application.DTOs.Item;
using SGE.Application.Interfaces.Repositories.Catalog;
using SGE.Application.Interfaces.Services.Catalog;
using SGE.Domain.Entities.Catalog;

namespace SGE.Application.Services.Catalog;

public class ItemService : IItemService
{
    private readonly IItemRepository _repository;
    private readonly ICategoryRepository _categoryRepository;

    public ItemService(
        IItemRepository repository,
        ICategoryRepository categoryRepository)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<ItemDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();

        return items.Select(MapToDto);
    }

    public async Task<ItemDto?> GetByIdAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);

        return item == null ? null : MapToDto(item);
    }

    public async Task<ItemDto> CreateAsync(CreateItemDto dto)
    {
        if (dto.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId.Value);

            if (category == null)
                throw new ArgumentException("A categoria informada nao existe.");
        }

        var item = new Item(
            dto.CategoryId,
            NormalizeCode(dto.Code),
            dto.Description.Trim(),
            dto.Unit.Trim(),
            dto.IsActive);

        await _repository.AddAsync(item);
        await _repository.SaveChangesAsync();

        return MapToDto(item);
    }

    public async Task<ItemDto?> UpdateAsync(
        Guid id,
        UpdateItemDto dto)
    {
        var item = await _repository.GetByIdAsync(id);

        if (item == null)
            return null;

        item.Update(
            NormalizeCode(dto.Code),
            dto.Description.Trim(),
            dto.Unit.Trim(),
            dto.IsActive);

        _repository.Update(item);
        await _repository.SaveChangesAsync();

        return MapToDto(item);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var item = await _repository.GetByIdAsync(id);

        if (item == null)
            return false;

        item.Deactivate();
        _repository.Update(item);
        await _repository.SaveChangesAsync();

        return true;
    }

    private static ItemDto MapToDto(Item item)
    {
        return new ItemDto
        {
            Id = item.Id,
            CategoryId = item.CategoryId,
            Code = item.Code,
            Description = item.Description,
            Unit = item.Unit,
            IsActive = item.IsActive
        };
    }

    private static string NormalizeCode(string code)
    {
        if (!string.IsNullOrWhiteSpace(code))
            return code.Trim();

        return $"MAT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
    }
}
