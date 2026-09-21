using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.Approval;
using SGE.Application.DTOs.PurchaseRequest;
using SGE.Application.Interfaces.Services.Authentication;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Security;
using SGE.Domain.Enums;

namespace SGE.API.Controllers.Purchasing;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchaseRequestController : ControllerBase
{
    private readonly IPurchaseRequestService _service;
    private readonly ICurrentUserService _currentUserService;

    public PurchaseRequestController(
        IPurchaseRequestService service,
        ICurrentUserService currentUserService)
    {
        _service = service;
        _currentUserService = currentUserService;
    }

    // GET: api/PurchaseRequest
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PurchaseRequestDto>>> GetAll()
    {
        var purchaseRequests = await _service.GetAllAsync();

        return Ok(purchaseRequests);
    }

    // GET: api/PurchaseRequest/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseRequestDto>> GetById(Guid id)
    {
        var purchaseRequest = await _service.GetByIdAsync(id);

        if (purchaseRequest == null)
            return NotFound();

        return Ok(purchaseRequest);
    }

    // POST: api/PurchaseRequest
    [Authorize(Roles = AppRoles.PurchaseRequestCreators)]
    [HttpPost]
    public async Task<ActionResult<PurchaseRequestDto>> Create(
        [FromBody] CreatePurchaseRequestDto dto)
    {
        try
        {
            if (!CanManageRequestType(dto.Type))
                return Forbid();

            dto.RequestedByUserId = _currentUserService.UserId;
            var purchaseRequest = await _service.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = purchaseRequest.Id },
                purchaseRequest);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
    // POST: api/PurchaseRequest/{id}/send-to-approval
    [Authorize(Roles = AppRoles.PurchaseRequestCreators)]
    [HttpPost("{id:guid}/send-to-approval")]
    public async Task<ActionResult<PurchaseRequestDto>> SendToApproval(Guid id)
    {
        return await SubmitForApproval(id);
    }

    [Authorize(Roles = AppRoles.PurchaseRequestCreators)]
    [HttpPost("{id:guid}/submit-for-approval")]
    public async Task<ActionResult<PurchaseRequestDto>> SubmitForApproval(Guid id)
    {
        try
        {
            var current = await _service.GetByIdAsync(id);

            if (current == null)
                return NotFound();

            if (!CanManageRequestType(current.Type))
                return Forbid();

            var purchaseRequest = await _service.SubmitForApprovalAsync(id);

            if (purchaseRequest == null)
                return NotFound();

            return Ok(purchaseRequest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize(Roles = AppRoles.PurchaseRequestCreators)]
    [HttpPost("{id:guid}/send-to-quotation")]
    public async Task<ActionResult<PurchaseRequestDto>> SendToQuotation(Guid id)
    {
        try
        {
            var current = await _service.GetByIdAsync(id);

            if (current == null)
                return NotFound();

            if (current.Type != PurchaseRequestType.Material || !CanManageRequestType(current.Type))
                return Forbid();

            var purchaseRequest = await _service.SendToQuotationAsync(id);

            if (purchaseRequest == null)
                return NotFound();

            return Ok(purchaseRequest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<PurchaseRequestDto>> Approve(
        Guid id,
        [FromBody] ApprovalDecisionDto dto)
    {
        try
        {
            dto.UserId = _currentUserService.UserId;
            var purchaseRequest = await _service.ApproveAsync(id, dto);

            if (purchaseRequest == null)
                return NotFound();

            return Ok(purchaseRequest);
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
    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<PurchaseRequestDto>> Reject(
        Guid id,
        [FromBody] ApprovalDecisionDto dto)
    {
        try
        {
            dto.UserId = _currentUserService.UserId;
            var purchaseRequest = await _service.RejectAsync(id, dto);

            if (purchaseRequest == null)
                return NotFound();

            return Ok(purchaseRequest);
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
    // PUT: api/PurchaseRequest/{id}
    [Authorize(Roles = AppRoles.PurchaseRequestCreators)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PurchaseRequestDto>> Update(
        Guid id,
        [FromBody] UpdatePurchaseRequestDto dto)
    {
        try
        {
            var current = await _service.GetByIdAsync(id);

            if (current == null)
                return NotFound();

            if (!CanManageRequestType(current.Type))
                return Forbid();

            var purchaseRequest = await _service.UpdateAsync(id, dto);

            if (purchaseRequest == null)
                return NotFound();

            return Ok(purchaseRequest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // DELETE: api/PurchaseRequest/{id}
    [Authorize(Roles = AppRoles.PurchaseRequestCreators)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    private bool CanManageRequestType(PurchaseRequestType type)
    {
        return type switch
        {
            PurchaseRequestType.Material => User.IsInRole(AppRoles.Warehouse) ||
                User.IsInRole(AppRoles.Buyer) ||
                User.IsInRole(AppRoles.Admin),
            PurchaseRequestType.Service => User.IsInRole(AppRoles.Warehouse) ||
                User.IsInRole(AppRoles.Buyer) ||
                User.IsInRole(AppRoles.Admin),
            _ => false
        };
    }

    [Authorize(Roles = AppRoles.PurchaseRequestCreators)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<PurchaseRequestDto>> Cancel(Guid id)
    {
        try
        {
            var current = await _service.GetByIdAsync(id);
            if (current == null) return NotFound();
            if (current.Type != PurchaseRequestType.Service || !CanManageRequestType(current.Type)) return Forbid();
            var cancelled = await _service.CancelAsync(id);
            return cancelled == null ? NotFound() : Ok(cancelled);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
