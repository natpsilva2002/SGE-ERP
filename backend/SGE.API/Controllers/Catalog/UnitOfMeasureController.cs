using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.UnitOfMeasure;
using SGE.Application.Interfaces.Services.Catalog;
using SGE.Application.Security;

namespace SGE.API.Controllers.Catalog;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UnitOfMeasureController : ControllerBase
{
    private readonly IUnitOfMeasureService _service;

    public UnitOfMeasureController(IUnitOfMeasureService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UnitOfMeasureDto>>> GetAll([FromQuery] bool activeOnly = false)
        => Ok(await _service.GetAllAsync(activeOnly));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnitOfMeasureDto>> GetById(Guid id)
    {
        var unit = await _service.GetByIdAsync(id);
        return unit == null ? NotFound() : Ok(unit);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<UnitOfMeasureDto>> Create(CreateUnitOfMeasureDto dto)
    {
        try
        {
            var unit = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = unit.Id }, unit);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UnitOfMeasureDto>> Update(Guid id, UpdateUnitOfMeasureDto dto)
    {
        try
        {
            var unit = await _service.UpdateAsync(id, dto);
            return unit == null ? NotFound() : Ok(unit);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}
