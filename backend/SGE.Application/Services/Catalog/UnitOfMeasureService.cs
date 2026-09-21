using SGE.Application.DTOs.UnitOfMeasure;
using SGE.Application.Interfaces.Repositories.Catalog;
using SGE.Application.Interfaces.Services.Catalog;
using SGE.Domain.Entities.Catalog;

namespace SGE.Application.Services.Catalog;

public class UnitOfMeasureService : IUnitOfMeasureService
{
    private readonly IUnitOfMeasureRepository _repository;

    public UnitOfMeasureService(IUnitOfMeasureRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<UnitOfMeasureDto>> GetAllAsync(bool activeOnly = false)
    {
        var units = await _repository.GetAllAsync();
        return units
            .Where(unit => !activeOnly || unit.IsActive)
            .OrderBy(unit => unit.Code)
            .Select(MapToDto);
    }

    public async Task<UnitOfMeasureDto?> GetByIdAsync(Guid id)
    {
        var unit = await _repository.GetByIdAsync(id);
        return unit == null ? null : MapToDto(unit);
    }

    public async Task<UnitOfMeasureDto> CreateAsync(CreateUnitOfMeasureDto dto)
    {
        var code = NormalizeCode(dto.Code);
        var description = NormalizeDescription(dto.Description);
        await EnsureCodeAvailableAsync(code, null);

        var unit = new UnitOfMeasure(code, description);
        await _repository.AddAsync(unit);
        await _repository.SaveChangesAsync();
        return MapToDto(unit);
    }

    public async Task<UnitOfMeasureDto?> UpdateAsync(Guid id, UpdateUnitOfMeasureDto dto)
    {
        var unit = await _repository.GetByIdAsync(id);
        if (unit == null)
            return null;

        var code = NormalizeCode(dto.Code);
        var description = NormalizeDescription(dto.Description);
        await EnsureCodeAvailableAsync(code, id);
        unit.Update(code, description, dto.IsActive);
        _repository.Update(unit);
        await _repository.SaveChangesAsync();
        return MapToDto(unit);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var unit = await _repository.GetByIdAsync(id);
        if (unit == null)
            return false;

        unit.Deactivate();
        _repository.Update(unit);
        await _repository.SaveChangesAsync();
        return true;
    }

    private async Task EnsureCodeAvailableAsync(string code, Guid? currentId)
    {
        var matches = await _repository.FindAsync(unit => unit.Code == code);
        if (matches.Any(unit => unit.Id != currentId))
            throw new ArgumentException("Ja existe uma unidade com este codigo.");
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("O codigo/sigla da unidade e obrigatorio.");

        return code.Trim().ToUpperInvariant();
    }

    private static string NormalizeDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("A descricao da unidade e obrigatoria.");

        return description.Trim();
    }

    private static UnitOfMeasureDto MapToDto(UnitOfMeasure unit) => new()
    {
        Id = unit.Id,
        Code = unit.Code,
        Description = unit.Description,
        IsActive = unit.IsActive
    };
}
