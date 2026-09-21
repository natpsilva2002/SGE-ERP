using SGE.Application.DTOs.User;
using SGE.Application.Interfaces.Repositories.Administration;
using SGE.Application.Interfaces.Services.Authentication;
using SGE.Application.Security;
using SGE.Domain.Entities.Administration;

namespace SGE.Application.Services.Authentication;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailWithRoleAsync(dto.Email);

        if (user == null ||
            !user.IsActive ||
            !IsPasswordValid(dto.Password, user.PasswordHash))
            throw new InvalidOperationException("Email ou senha invalidos.");

        var token = _jwtTokenService.GenerateToken(user);

        return new LoginResponseDto
        {
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            User = MapUser(user)
        };
    }

    private bool IsPasswordValid(string password, string passwordHash)
    {
        try
        {
            return _passwordHasher.VerifyPassword(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }

    private static AuthUserDto MapUser(User user)
    {
        return new AuthUserDto
        {
            Id = user.Id,
            Name = $"{user.FirstName} {user.LastName}".Trim(),
            Email = user.Email,
            RoleId = user.RoleId,
            Role = AppRoles.Normalize(user.Role.Name)
        };
    }
}
