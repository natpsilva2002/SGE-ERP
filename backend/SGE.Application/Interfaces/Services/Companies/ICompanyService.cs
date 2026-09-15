using SGE.Application.DTOs.Company;

namespace SGE.Application.Interfaces.Services.Companies;

public interface ICompanyService
{
    Task<IEnumerable<CompanyDto>> GetAllAsync();

    Task<CompanyDto?> GetByIdAsync(Guid id);

    Task<CompanyDto> CreateAsync(CreateCompanyDto dto);

    Task<CompanyDto?> UpdateAsync(
        Guid id,
        UpdateCompanyDto dto);

    Task<bool> DeleteAsync(Guid id);
}