using SGE.Application.DTOs.Work;
using SGE.Application.Interfaces.Repositories.Companies;
using SGE.Application.Interfaces.Services.Companies;
using SGE.Domain.Entities.Companies;

namespace SGE.Application.Services.Companies;

public class WorkService : IWorkService
{
    private readonly IWorkRepository _repository;
    private readonly ICompanyRepository _companyRepository;

    public WorkService(
        IWorkRepository repository,
        ICompanyRepository companyRepository)
    {
        _repository = repository;
        _companyRepository = companyRepository;
    }

    public async Task<IEnumerable<WorkDto>> GetAllAsync()
    {
        var works = await _repository.GetAllAsync();

        return works.Select(MapToDto);
    }

    public async Task<WorkDto?> GetByIdAsync(Guid id)
    {
        var work = await _repository.GetByIdAsync(id);

        if (work == null)
            return null;

        return MapToDto(work);
    }

    public async Task<WorkDto> CreateAsync(CreateWorkDto dto)
    {
        var company = await _companyRepository.GetByIdAsync(dto.CompanyId);

        if (company == null)
            throw new ArgumentException("A empresa informada não existe.");

        var startDate = dto.StartDate.Kind == DateTimeKind.Unspecified
    ? DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc)
    : dto.StartDate.ToUniversalTime();

        var work = new Work(
            dto.CompanyId,
            dto.Code,
            dto.Name,
            dto.Description,
            startDate);

        await _repository.AddAsync(work);
        await _repository.SaveChangesAsync();

        return MapToDto(work);
    }

    public async Task<WorkDto?> UpdateAsync(
        Guid id,
        UpdateWorkDto dto)
    {
        var work = await _repository.GetByIdAsync(id);

        if (work == null)
            return null;

        work.Update(
            dto.Code,
            dto.Name,
            dto.Description);

        _repository.Update(work);
        await _repository.SaveChangesAsync();

        return MapToDto(work);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var work = await _repository.GetByIdAsync(id);

        if (work == null)
            return false;

        _repository.Remove(work);
        await _repository.SaveChangesAsync();

        return true;
    }

    private static WorkDto MapToDto(Work work)
    {
        return new WorkDto
        {
            Id = work.Id,
            CompanyId = work.CompanyId,
            Code = work.Code,
            Name = work.Name,
            Description = work.Description,
            StartDate = work.StartDate,
            EndDate = work.EndDate
        };
    }
}