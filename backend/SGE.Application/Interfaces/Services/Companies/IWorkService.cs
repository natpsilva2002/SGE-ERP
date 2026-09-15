using SGE.Application.DTOs.Work;

namespace SGE.Application.Interfaces.Services.Companies;

public interface IWorkService
{
    Task<IEnumerable<WorkDto>> GetAllAsync();

    Task<WorkDto?> GetByIdAsync(Guid id);

    Task<WorkDto> CreateAsync(CreateWorkDto dto);

    Task<WorkDto?> UpdateAsync(
        Guid id,
        UpdateWorkDto dto);

    Task<bool> DeleteAsync(Guid id);
}