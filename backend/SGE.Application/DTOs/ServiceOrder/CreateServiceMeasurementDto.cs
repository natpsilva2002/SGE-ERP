namespace SGE.Application.DTOs.ServiceOrder;

public class CreateServiceMeasurementDto
{
    public DateTime MeasurementDate { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? Observation { get; set; }

    public Guid CreatedByUserId { get; set; }
}
