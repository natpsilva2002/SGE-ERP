using SGE.Application.DTOs.Dashboard;

namespace SGE.Application.Interfaces.Repositories.Dashboard;

public interface IDashboardRepository
{
    Task<DashboardDto> GetAsync(string role);
}
