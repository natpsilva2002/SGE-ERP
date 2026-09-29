namespace SGE.Application.DTOs.ServiceOrder;

public class CreateServiceOrderAmendmentDto
{
    public string Reason { get; set; } = string.Empty;
    public decimal? ValueAdjustment { get; set; }
    public decimal? QuantityAdjustment { get; set; }
    public string? Observation { get; set; }
}
