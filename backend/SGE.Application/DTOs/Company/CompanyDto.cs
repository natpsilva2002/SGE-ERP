namespace SGE.Application.DTOs.Company;

public class CompanyDto
{
    public Guid Id { get; set; }

    public string CorporateName { get; set; } = string.Empty;

    public string TradeName { get; set; } = string.Empty;

    public string Document { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;
}