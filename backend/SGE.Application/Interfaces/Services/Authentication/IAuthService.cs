using SGE.Application.DTOs.User;

namespace SGE.Application.Interfaces.Services.Authentication;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginDto dto);
}
