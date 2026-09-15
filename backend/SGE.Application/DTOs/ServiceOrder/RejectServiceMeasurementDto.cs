namespace SGE.Application.DTOs.ServiceOrder;

public class RejectServiceMeasurementDto
{
    public string RejectionReason { get; set; } = string.Empty;

    public Guid RejectedByUserId { get; set; }
}
