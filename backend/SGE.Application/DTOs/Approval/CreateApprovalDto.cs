using SGE.Domain.Enums;

namespace SGE.Application.DTOs.Approval;

public class CreateApprovalDto
{
    public ApprovalType Type { get; set; }

    public Guid? PurchaseRequestId { get; set; }

    public Guid? QuotationId { get; set; }

    public Guid UserId { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    public string? Observation { get; set; }
}
