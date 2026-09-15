using SGE.Domain.Enums;

namespace SGE.Application.DTOs.Approval;

public class ApprovalDto
{
    public Guid Id { get; set; }

    public ApprovalType Type { get; set; }

    public ApprovalStatus Status { get; set; }

    public Guid? PurchaseRequestId { get; set; }

    public Guid? QuotationId { get; set; }

    public Guid UserId { get; set; }

    public string? Observation { get; set; }

    public DateTime? ApprovalDate { get; set; }
}
