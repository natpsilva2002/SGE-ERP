using SGE.Application.DTOs.Item;
using SGE.Application.Interfaces.Repositories.Catalog;
using SGE.Application.Interfaces.Services.Catalog;
using SGE.Domain.Entities.Catalog;

namespace SGE.Application.Services.Catalog;

public class ItemService : IItemService
{
    private readonly IItemRepository _repository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfMeasureRepository _unitOfMeasureRepository;

    public ItemService(
        IItemRepository repository,
        ICategoryRepository categoryRepository,
        IUnitOfMeasureRepository unitOfMeasureRepository)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _unitOfMeasureRepository = unitOfMeasureRepository;
    }

    public async Task<IEnumerable<ItemDto>> GetAllAsync()
    {
        var items = await _repository.GetAllAsync();

        return items.OrderByDescending(x => x.CreatedAt).Select(MapToDto);
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

        var unit = await ResolveUnitAsync(dto.UnitOfMeasureId, dto.Unit);
        var item = new Item(
            dto.CategoryId,
            NormalizeCode(dto.Code),
            dto.Description.Trim(),
            unit.Code,
            dto.IsActive,
            unit.Id == Guid.Empty ? null : unit.Id);

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

        var unit = await ResolveUnitAsync(dto.UnitOfMeasureId, dto.Unit);
        item.Update(
            NormalizeCode(dto.Code),
            dto.Description.Trim(),
            unit.Code,
            dto.IsActive,
            unit.Id == Guid.Empty ? null : unit.Id);

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
            UnitOfMeasureId = item.UnitOfMeasureId,
            IsActive = item.IsActive
        };
    }

    private static string NormalizeCode(string code)
    {
        if (!string.IsNullOrWhiteSpace(code))
            return code.Trim();

        return $"MAT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
    }

    private async Task<UnitOfMeasure> ResolveUnitAsync(Guid? unitOfMeasureId, string legacyUnit)
    {
        if (unitOfMeasureId.HasValue)
        {
            var unit = await _unitOfMeasureRepository.GetByIdAsync(unitOfMeasureId.Value);
            if (unit == null || !unit.IsActive)
                throw new ArgumentException("A unidade de medida informada nao existe ou esta inativa.");

            return unit;
        }

        if (string.IsNullOrWhiteSpace(legacyUnit))
            throw new ArgumentException("A unidade de medida e obrigatoria.");

        var legacy = new UnitOfMeasure(legacyUnit.Trim(), legacyUnit.Trim());
        legacy.Id = Guid.Empty;
        return legacy;
    }
}
