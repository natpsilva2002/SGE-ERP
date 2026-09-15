namespace SGE.Application.DTOs.User;

public class JwtTokenDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}
