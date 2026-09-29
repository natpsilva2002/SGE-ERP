using SGE.Domain.Enums;

namespace SGE.Application.DTOs.ServiceOrder;

public class ServiceOrderAmendmentDto
{
    public Guid Id { get; set; }
    public Guid ServiceOrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal? ValueAdjustment { get; set; }
    public decimal? QuantityAdjustment { get; set; }
    public string? Observation { get; set; }
    public ServiceOrderAmendmentStatus Status { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public decimal? ValueBeforeApproval { get; set; }
    public decimal? ValueAfterApproval { get; set; }
    public decimal? QuantityBeforeApproval { get; set; }
    public decimal? QuantityAfterApproval { get; set; }
    public ApprovalStatus? ApprovalStatus { get; set; }
    public IEnumerable<ServiceOrderAmendmentAttachmentDto> Attachments { get; set; } = [];
}
