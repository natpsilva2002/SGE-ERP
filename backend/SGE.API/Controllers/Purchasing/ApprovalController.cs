using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.Approval;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Security;

namespace SGE.API.Controllers.Purchasing;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.ApproverOrAdmin)]
public class ApprovalController : ControllerBase
{
    private readonly IApprovalService _service;

    public ApprovalController(IApprovalService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApprovalDto>>> GetAll()
    {
        var approvals = await _service.GetAllAsync();

        return Ok(approvals);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApprovalDto>> GetById(Guid id)
    {
        var approval = await _service.GetByIdAsync(id);

        if (approval == null)
            return NotFound();

        return Ok(approval);
    }

}
