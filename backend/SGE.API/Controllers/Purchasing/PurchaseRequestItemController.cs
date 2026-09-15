using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.PurchaseRequestItem;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Security;

namespace SGE.API.Controllers.Purchasing;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchaseRequestItemController : ControllerBase
{
    private readonly IPurchaseRequestItemService _service;

    public PurchaseRequestItemController(
        IPurchaseRequestItemService service)
    {
        _service = service;
    }

    // GET: api/PurchaseRequestItem
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PurchaseRequestItemDto>>> GetAll()
    {
        var items = await _service.GetAllAsync();

        return Ok(items);
    }

    // GET: api/PurchaseRequestItem/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseRequestItemDto>> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    // POST: api/PurchaseRequestItem
    [Authorize(Roles = AppRoles.RequesterOrAdmin)]
    [HttpPost]
    public async Task<ActionResult<PurchaseRequestItemDto>> Create(
        [FromBody] CreatePurchaseRequestItemDto dto)
    {
        try
        {
            var item = await _service.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = item.Id },
                item);
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

    // PUT: api/PurchaseRequestItem/{id}
    [Authorize(Roles = AppRoles.RequesterOrAdmin)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PurchaseRequestItemDto>> Update(
        Guid id,
        [FromBody] UpdatePurchaseRequestItemDto dto)
    {
        try
        {
            var item = await _service.UpdateAsync(id, dto);

            if (item == null)
                return NotFound();

            return Ok(item);
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

    // DELETE: api/PurchaseRequestItem/{id}
    [Authorize(Roles = AppRoles.RequesterOrAdmin)]
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
}
