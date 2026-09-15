using SGE.Domain.Enums;

namespace SGE.Application.DTOs.ServiceOrder;

public class ServiceAdvancePaymentRequestDto
{
    public Guid Id { get; set; }

    public Guid ServiceOrderId { get; set; }

    public Guid RequestedByUserId { get; set; }

    public string? RequestedByUserName { get; set; }

    public DateTime RequestedAt { get; set; }

    public decimal Amount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal AmountPending { get; set; }

    public string? Observation { get; set; }

    public ServiceAdvancePaymentStatus Status { get; set; }

    public Guid? ApprovedByUserId { get; set; }

    public string? ApprovedByUserName { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid? RejectedByUserId { get; set; }

    public string? RejectedByUserName { get; set; }

    public DateTime? RejectedAt { get; set; }

    public string? RejectionReason { get; set; }
}
