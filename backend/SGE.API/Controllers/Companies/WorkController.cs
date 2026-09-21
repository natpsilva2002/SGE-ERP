using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.Work;
using SGE.Application.Interfaces.Services.Companies;
using SGE.Application.Security;

namespace SGE.API.Controllers.Companies;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkController : ControllerBase
{
    private readonly IWorkService _service;

    public WorkController(IWorkService service)
    {
        _service = service;
    }

    // GET: api/Work
    [Authorize(Roles = AppRoles.Buyer + "," + AppRoles.Warehouse + "," + AppRoles.Finance + "," + AppRoles.Admin)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkDto>>> GetAll()
    {
        var works = await _service.GetAllAsync();

        return Ok(works);
    }

    // GET: api/Work/{id}
    [Authorize(Roles = AppRoles.Buyer + "," + AppRoles.Warehouse + "," + AppRoles.Finance + "," + AppRoles.Admin)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkDto>> GetById(Guid id)
    {
        var work = await _service.GetByIdAsync(id);

        if (work == null)
            return NotFound();

        return Ok(work);
    }

    // POST: api/Work
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<WorkDto>> Create(
        [FromBody] CreateWorkDto dto)
    {
        try
        {
            var work = await _service.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = work.Id },
                work);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // PUT: api/Work/{id}
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkDto>> Update(
        Guid id,
        [FromBody] UpdateWorkDto dto)
    {
        var work = await _service.UpdateAsync(id, dto);

        if (work == null)
            return NotFound();

        return Ok(work);
    }

    // DELETE: api/Work/{id}
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
