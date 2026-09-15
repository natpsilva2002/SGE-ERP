using SGE.Application.DTOs.Supplier;

namespace SGE.Application.Interfaces.Services.Companies;

public interface ISupplierService
{
    Task<IEnumerable<SupplierDto>> GetAllAsync();

    Task<SupplierDto?> GetByIdAsync(Guid id);

    Task<SupplierDto> CreateAsync(CreateSupplierDto dto);

    Task<SupplierDto?> UpdateAsync(
        Guid id,
        UpdateSupplierDto dto);

    Task<bool> DeleteAsync(Guid id);
}