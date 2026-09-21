using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.ApprovalHistory;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Security;

namespace SGE.API.Controllers.Purchasing;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Admin)]
public class ApprovalHistoryController : ControllerBase
{
    private readonly IApprovalHistoryService _service;

    public ApprovalHistoryController(IApprovalHistoryService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApprovalHistoryDto>>> GetAll()
    {
        var histories = await _service.GetAllAsync();

        return Ok(histories);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApprovalHistoryDto>> GetById(Guid id)
    {
        var history = await _service.GetByIdAsync(id);

        if (history == null)
            return NotFound();

        return Ok(history);
    }

}
