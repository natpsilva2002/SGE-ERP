using SGE.Application.DTOs.Company;
using SGE.Application.Interfaces.Repositories.Companies;
using SGE.Application.Interfaces.Services.Companies;
using SGE.Domain.Entities.Companies;

namespace SGE.Application.Services.Companies;

public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _repository;

    public CompanyService(ICompanyRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<CompanyDto>> GetAllAsync()
    {
        var companies = await _repository.GetAllAsync();

        return companies.Select(MapToDto);
    }

    public async Task<CompanyDto?> GetByIdAsync(Guid id)
    {
        var company = await _repository.GetByIdAsync(id);

        return company == null
            ? null
            : MapToDto(company);
    }

    public async Task<CompanyDto> CreateAsync(CreateCompanyDto dto)
    {
        var company = new Company(
            dto.CorporateName,
            dto.TradeName,
            dto.Document,
            dto.Email,
            dto.Phone);

        await _repository.AddAsync(company);
        await _repository.SaveChangesAsync();

        return MapToDto(company);
    }

    public async Task<CompanyDto?> UpdateAsync(
        Guid id,
        UpdateCompanyDto dto)
    {
        var company = await _repository.GetByIdAsync(id);

        if (company == null)
            return null;

        company.Update(
            dto.CorporateName,
            dto.TradeName,
            dto.Email,
            dto.Phone);

        _repository.Update(company);

        await _repository.SaveChangesAsync();

        return MapToDto(company);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var company = await _repository.GetByIdAsync(id);

        if (company == null)
            return false;

        _repository.Remove(company);

        await _repository.SaveChangesAsync();

        return true;
    }

    private static CompanyDto MapToDto(Company company)
    {
        return new CompanyDto
        {
            Id = company.Id,
            CorporateName = company.CorporateName,
            TradeName = company.TradeName,
            Document = company.Document,
            Email = company.Email,
            Phone = company.Phone
        };
    }
}