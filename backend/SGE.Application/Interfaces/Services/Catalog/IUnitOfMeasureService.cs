using SGE.Application.DTOs.UnitOfMeasure;

namespace SGE.Application.Interfaces.Services.Catalog;

public interface IUnitOfMeasureService
{
    Task<IEnumerable<UnitOfMeasureDto>> GetAllAsync(bool activeOnly = false);

    Task<UnitOfMeasureDto?> GetByIdAsync(Guid id);

    Task<UnitOfMeasureDto> CreateAsync(CreateUnitOfMeasureDto dto);

    Task<UnitOfMeasureDto?> UpdateAsync(Guid id, UpdateUnitOfMeasureDto dto);

    Task<bool> DeleteAsync(Guid id);
}
