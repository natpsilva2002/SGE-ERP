using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SGE.Application.DTOs.User;
using SGE.Application.Interfaces.Repositories.Administration;
using SGE.Application.Interfaces.Services.Authentication;

namespace SGE.API.Controllers.Authentication;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;

    public AuthController(
        IAuthService authService,
        ICurrentUserService currentUserService,
        IUserRepository userRepository)
    {
        _authService = authService;
        _currentUserService = currentUserService;
        _userRepository = userRepository;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginDto dto)
    {
        try
        {
            var response = await _authService.LoginAsync(dto);

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthUserDto>> Me()
    {
        var user = await _userRepository.GetByIdWithRoleAsync(
            _currentUserService.UserId);

        if (user == null)
            return NotFound(new
            {
                message = "Usuario autenticado nao encontrado."
            });

        return Ok(new AuthUserDto
        {
            Id = user.Id,
            Name = $"{user.FirstName} {user.LastName}".Trim(),
            Email = user.Email,
            RoleId = user.RoleId,
            Role = user.Role.Name
        });
    }
}
