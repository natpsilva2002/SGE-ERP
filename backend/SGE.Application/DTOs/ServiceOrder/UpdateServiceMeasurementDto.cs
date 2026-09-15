namespace SGE.Application.DTOs.ServiceOrder;

public class UpdateServiceMeasurementDto
{
    public DateTime MeasurementDate { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? Observation { get; set; }
}
