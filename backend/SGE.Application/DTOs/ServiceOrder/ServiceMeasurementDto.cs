using SGE.Domain.Enums;

namespace SGE.Application.DTOs.ServiceOrder;

public class ServiceMeasurementDto
{
    public Guid Id { get; set; }

    public Guid ServiceOrderId { get; set; }

    public string MeasurementNumber { get; set; } = string.Empty;

    public DateTime MeasurementDate { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal QuantityMeasured { get; set; }

    public string Unit { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? Observation { get; set; }

    public ServiceMeasurementStatus Status { get; set; }

    public Guid CreatedByUserId { get; set; }

    public string? CreatedByUserName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid? ApprovedByUserId { get; set; }

    public string? ApprovedByUserName { get; set; }

    public DateTime? RejectedAt { get; set; }

    public Guid? RejectedByUserId { get; set; }

    public string? RejectedByUserName { get; set; }

    public string? RejectionReason { get; set; }

    public List<ServiceMeasurementAttachmentDto> Attachments { get; set; } = new();
}
