using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.Finance;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Security;

namespace SGE.API.Controllers.Finance;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.FinanceOrAdmin)]
public class FinanceController : ControllerBase
{
    private readonly IFinanceQueueService _service;

    public FinanceController(IFinanceQueueService service)
    {
        _service = service;
    }

    [HttpGet("queue")]
    public async Task<ActionResult<IEnumerable<FinanceQueueItemDto>>> GetQueue()
    {
        return Ok(await _service.GetAllAsync());
    }
}
