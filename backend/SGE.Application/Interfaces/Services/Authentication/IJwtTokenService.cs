using SGE.Application.DTOs.User;
using SGE.Domain.Entities.Administration;

namespace SGE.Application.Interfaces.Services.Authentication;

public interface IJwtTokenService
{
    JwtTokenDto GenerateToken(User user);
}
