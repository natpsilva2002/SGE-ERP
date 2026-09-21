using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.Supplier;
using SGE.Application.Interfaces.Services.Companies;
using SGE.Application.Security;

namespace SGE.API.Controllers.Companies;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SupplierController : ControllerBase
{
    private readonly ISupplierService _service;

    public SupplierController(ISupplierService service)
    {
        _service = service;
    }

    // GET: api/Supplier
    [Authorize(Roles = AppRoles.SupplierReaders)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SupplierDto>>> GetAll()
    {
        var suppliers = await _service.GetAllAsync();

        return Ok(suppliers);
    }

    // GET: api/Supplier/{id}
    [Authorize(Roles = AppRoles.SupplierReaders)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> GetById(Guid id)
    {
        var supplier = await _service.GetByIdAsync(id);

        if (supplier == null)
            return NotFound();

        return Ok(supplier);
    }

    // POST: api/Supplier
    [Authorize(Roles = AppRoles.BuyerOrAdmin)]
    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create(
        [FromBody] CreateSupplierDto dto)
    {
        try
        {
            var supplier = await _service.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = supplier.Id },
                supplier);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // PUT: api/Supplier/{id}
    [Authorize(Roles = AppRoles.BuyerOrAdmin)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> Update(
        Guid id,
        [FromBody] UpdateSupplierDto dto)
    {
        var supplier = await _service.UpdateAsync(id, dto);

        if (supplier == null)
            return NotFound();

        return Ok(supplier);
    }

    // DELETE: api/Supplier/{id}
    [Authorize(Roles = AppRoles.BuyerOrAdmin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
