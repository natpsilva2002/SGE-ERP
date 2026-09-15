using SGE.Application.DTOs.Item;

namespace SGE.Application.Interfaces.Services.Catalog;

public interface IItemService
{
    Task<IEnumerable<ItemDto>> GetAllAsync();

    Task<ItemDto?> GetByIdAsync(Guid id);

    Task<ItemDto> CreateAsync(CreateItemDto dto);

    Task<ItemDto?> UpdateAsync(Guid id, UpdateItemDto dto);

    Task<bool> DeleteAsync(Guid id);
}