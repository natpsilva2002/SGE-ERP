using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.Role;
using SGE.Application.Interfaces.Services.Administration;
using SGE.Application.Security;

namespace SGE.API.Controllers.Administration;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Admin)]
public class RoleController : ControllerBase
{
    private readonly IRoleService _service;

    public RoleController(IRoleService service)
    {
        _service = service;
    }

    // GET: api/Role
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetAll()
    {
        var roles = await _service.GetAllAsync();

        return Ok(roles);
    }

    // GET: api/Role/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleDto>> GetById(Guid id)
    {
        var role = await _service.GetByIdAsync(id);

        if (role == null)
            return NotFound();

        return Ok(role);
    }

}
