using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.Company;
using SGE.Application.Interfaces.Services.Companies;
using SGE.Application.Security;

namespace SGE.API.Controllers.Companies;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _service;

    public CompanyController(ICompanyService service)
    {
        _service = service;
    }

    // GET: api/Company
    [Authorize(Roles = AppRoles.Buyer + "," + AppRoles.Warehouse + "," + AppRoles.Finance + "," + AppRoles.Admin)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompanyDto>>> GetAll()
    {
        var companies = await _service.GetAllAsync();

        return Ok(companies);
    }

    // GET: api/Company/{id}
    [Authorize(Roles = AppRoles.Buyer + "," + AppRoles.Warehouse + "," + AppRoles.Finance + "," + AppRoles.Admin)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyDto>> GetById(Guid id)
    {
        var company = await _service.GetByIdAsync(id);

        if (company == null)
            return NotFound();

        return Ok(company);
    }

    // POST: api/Company
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<CompanyDto>> Create(
        [FromBody] CreateCompanyDto dto)
    {
        var company = await _service.CreateAsync(dto);

        return CreatedAtAction(
            nameof(GetById),
            new { id = company.Id },
            company);
    }

    // PUT: api/Company/{id}
    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CompanyDto>> Update(
        Guid id,
        [FromBody] UpdateCompanyDto dto)
    {
        var company = await _service.UpdateAsync(id, dto);

        if (company == null)
            return NotFound();

        return Ok(company);
    }

    // DELETE: api/Company/{id}
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
