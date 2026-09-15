namespace SGE.Application.DTOs.Supplier;

public class SupplierDto
{
    public Guid Id { get; set; }

    public Guid? CompanyId { get; set; }

    public string CorporateName { get; set; } = string.Empty;

    public string TradeName { get; set; } = string.Empty;

    public string Document { get; set; } = string.Empty;

    public string? StateRegistration { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string ContactName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Number { get; set; } = string.Empty;

    public string? Complement { get; set; }

    public string District { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string ZipCode { get; set; } = string.Empty;

    public string? PixKey { get; set; }

    public string? Bank { get; set; }

    public string? Agency { get; set; }

    public string? Account { get; set; }

    public bool IsActive { get; set; }
}
