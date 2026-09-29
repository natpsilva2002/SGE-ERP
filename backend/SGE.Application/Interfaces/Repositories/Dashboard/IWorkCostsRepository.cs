using SGE.Application.DTOs.Dashboard;

namespace SGE.Application.Interfaces.Repositories.Dashboard;

public interface IWorkCostsRepository
{
    Task<WorkCostsDto> GetAsync(
        Guid? workId,
        DateTime? startDate,
        DateTime? endDate);
}
