using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.QuotationItem;
using SGE.Application.Interfaces.Services.Purchasing;
using SGE.Application.Security;

namespace SGE.API.Controllers.Purchasing;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuotationItemController : ControllerBase
{
    private readonly IQuotationItemService _service;

    public QuotationItemController(IQuotationItemService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuotationItemDto>>> GetAll()
    {
        var items = await _service.GetAllAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuotationItemDto>> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);

        if (item == null)
            return NotFound();

        return Ok(item);
    }

    [Authorize(Roles = AppRoles.QuotationManagers)]
    [HttpPost]
    public async Task<ActionResult<QuotationItemDto>> Create(
        [FromBody] CreateQuotationItemDto dto)
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

    [Authorize(Roles = AppRoles.QuotationManagers)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<QuotationItemDto>> Update(
        Guid id,
        [FromBody] UpdateQuotationItemDto dto)
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

    [Authorize(Roles = AppRoles.QuotationManagers)]
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

    [Authorize(Roles = AppRoles.ApproverOrAdmin)]
    [HttpPatch("{id:guid}/select")]
    public async Task<ActionResult<QuotationItemDto>> Select(Guid id)
    {
        try
        {
            var item = await _service.SelectAsync(id);

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
}
