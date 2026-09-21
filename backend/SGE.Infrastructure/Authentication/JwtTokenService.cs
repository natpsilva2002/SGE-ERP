using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SGE.Application.DTOs.User;
using SGE.Application.Interfaces.Services.Authentication;
using SGE.Application.Security;
using SGE.Domain.Entities.Administration;

namespace SGE.Infrastructure.Authentication;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public JwtTokenDto GenerateToken(User user)
    {
        var key = _configuration["Jwt:Key"];
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var expirationMinutes = int.Parse(
            _configuration["Jwt:ExpirationMinutes"] ?? "60");

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Jwt:Key nao configurado.");

        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
        var name = $"{user.FirstName} {user.LastName}".Trim();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, name),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Role, AppRoles.Normalize(user.Role.Name))
        };

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtTokenDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt
        };
    }
}
