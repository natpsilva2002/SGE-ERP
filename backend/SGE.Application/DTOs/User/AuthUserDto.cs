namespace SGE.Application.DTOs.User;

public class AuthUserDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public Guid RoleId { get; set; }

    public string Role { get; set; } = string.Empty;
}
