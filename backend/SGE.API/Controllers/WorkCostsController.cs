using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.Dashboard;
using SGE.Application.Interfaces.Repositories.Dashboard;
using SGE.Application.Security;

namespace SGE.API.Controllers;

[ApiController]
[Route("api/Dashboard/work-costs")]
[Authorize(Roles = AppRoles.FinanceOrAdmin)]
public class WorkCostsController : ControllerBase
{
    private readonly IWorkCostsRepository _repository;

    public WorkCostsController(IWorkCostsRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public Task<WorkCostsDto> Get(
        [FromQuery] Guid? workId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate) =>
        _repository.GetAsync(workId, startDate, endDate);
}
