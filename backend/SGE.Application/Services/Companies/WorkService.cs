using SGE.Application.DTOs.Work;
using SGE.Application.Interfaces.Repositories.Companies;
using SGE.Application.Interfaces.Services.Companies;
using SGE.Domain.Entities.Companies;

namespace SGE.Application.Services.Companies;

public class WorkService : IWorkService
{
    private readonly IWorkRepository _repository;

    public WorkService(IWorkRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<WorkDto>> GetAllAsync()
    {
        var works = await _repository.GetAllAsync();

        return works.OrderByDescending(x => x.CreatedAt).Select(MapToDto);
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

        if (dto.IsActive.HasValue)
            work.SetActive(dto.IsActive.Value);

        _repository.Update(work);
        await _repository.SaveChangesAsync();

        return MapToDto(work);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var work = await _repository.GetByIdAsync(id);

        if (work == null)
            return false;

        work.Deactivate();
        _repository.Update(work);
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
            EndDate = work.EndDate,
            IsActive = work.IsActive
        };
    }
}
