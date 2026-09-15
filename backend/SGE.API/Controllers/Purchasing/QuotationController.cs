using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.Approval;
using SGE.Application.DTOs.Quotation;
using SGE.Application.Interfaces.Services.Authentication;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Security;

namespace SGE.API.Controllers.Purchasing;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuotationController : ControllerBase
{
    private readonly IQuotationService _service;
    private readonly ICurrentUserService _currentUserService;

    public QuotationController(
        IQuotationService service,
        ICurrentUserService currentUserService)
    {
        _service = service;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuotationDto>>> GetAll()
    {
        var quotations = await _service.GetAllAsync();

        return Ok(quotations);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuotationDto>> GetById(Guid id)
    {
        var quotation = await _service.GetByIdAsync(id);

        if (quotation == null)
            return NotFound();

        return Ok(quotation);
    }

    [Authorize(Roles = AppRoles.QuotationManagers)]
    [HttpPost]
    public async Task<ActionResult<QuotationDto>> Create(
        [FromBody] CreateQuotationDto dto)
    {
        try
        {
            var quotation = await _service.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = quotation.Id },
                quotation);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize(Roles = AppRoles.QuotationManagers)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<QuotationDto>> Update(
        Guid id,
        [FromBody] UpdateQuotationDto dto)
    {
        try
        {
            var quotation = await _service.UpdateAsync(id, dto);

            if (quotation == null)
                return NotFound();

            return Ok(quotation);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize(Roles = AppRoles.QuotationManagers)]
    [HttpPost("{id:guid}/submit-for-approval")]
    public async Task<ActionResult<QuotationDto>> SubmitForApproval(Guid id)
    {
        try
        {
            var quotation = await _service.SubmitForApprovalAsync(id);

            if (quotation == null)
                return NotFound();

            return Ok(quotation);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize(Roles = AppRoles.ApproverOrAdmin)]
    [HttpPatch("{id:guid}/select-supplier/{supplierId:guid}")]
    public async Task<ActionResult<QuotationDto>> SelectSupplier(
        Guid id,
        Guid supplierId)
    {
        try
        {
            var quotation = await _service.SelectSupplierAsync(id, supplierId);

            if (quotation == null)
                return NotFound();

            return Ok(quotation);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize(Roles = AppRoles.ApproverOrAdmin)]
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<QuotationApprovalResultDto>> Approve(
        Guid id,
        [FromBody] ApprovalDecisionDto dto)
    {
        try
        {
            dto.UserId = _currentUserService.UserId;
            var quotation = await _service.ApproveAsync(id, dto);

            if (quotation == null)
                return NotFound();

            return Ok(quotation);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize(Roles = AppRoles.ApproverOrAdmin)]
    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<QuotationDto>> Reject(
        Guid id,
        [FromBody] ApprovalDecisionDto dto)
    {
        try
        {
            dto.UserId = _currentUserService.UserId;
            var quotation = await _service.RejectAsync(id, dto);

            if (quotation == null)
                return NotFound();

            return Ok(quotation);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
