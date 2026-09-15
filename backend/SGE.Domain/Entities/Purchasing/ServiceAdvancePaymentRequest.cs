using SGE.Domain.Common;
using SGE.Domain.Entities.Administration;
using SGE.Domain.Enums;

namespace SGE.Domain.Entities.Purchasing;

public class ServiceAdvancePaymentRequest : BaseEntity
{
    public Guid ServiceOrderId { get; private set; }

    public Guid RequestedByUserId { get; private set; }

    public DateTime RequestedAt { get; private set; }

    public decimal Amount { get; private set; }

    public string? Observation { get; private set; }

    public ServiceAdvancePaymentStatus Status { get; private set; }

    public Guid? ApprovedByUserId { get; private set; }

    public DateTime? ApprovedAt { get; private set; }

    public Guid? RejectedByUserId { get; private set; }

    public DateTime? RejectedAt { get; private set; }

    public string? RejectionReason { get; private set; }

    public ServiceOrder ServiceOrder { get; private set; } = null!;

    public User RequestedByUser { get; private set; } = null!;

    public User? ApprovedByUser { get; private set; }

    public User? RejectedByUser { get; private set; }

    public ICollection<ServiceOrderPayment> Payments { get; private set; } = new List<ServiceOrderPayment>();

    private ServiceAdvancePaymentRequest()
    {
    }

    public ServiceAdvancePaymentRequest(
        Guid serviceOrderId,
        Guid requestedByUserId,
        decimal amount,
        string? observation)
    {
        if (amount <= 0)
            throw new ArgumentException("O valor da antecipacao deve ser maior que zero.");

        ServiceOrderId = serviceOrderId;
        RequestedByUserId = requestedByUserId;
        Amount = amount;
        Observation = observation;
        RequestedAt = DateTime.UtcNow;
        Status = ServiceAdvancePaymentStatus.WaitingApproval;
    }

    public void Approve(Guid approvedByUserId)
    {
        if (Status != ServiceAdvancePaymentStatus.WaitingApproval)
            throw new InvalidOperationException("Apenas antecipacoes aguardando aprovacao podem ser aprovadas.");

        ApprovedByUserId = approvedByUserId;
        ApprovedAt = DateTime.UtcNow;
        Status = ServiceAdvancePaymentStatus.Approved;
    }

    public void Reject(Guid rejectedByUserId, string rejectionReason)
    {
        if (Status != ServiceAdvancePaymentStatus.WaitingApproval)
            throw new InvalidOperationException("Apenas antecipacoes aguardando aprovacao podem ser rejeitadas.");

        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new ArgumentException("O motivo da rejeicao e obrigatorio.");

        RejectedByUserId = rejectedByUserId;
        RejectedAt = DateTime.UtcNow;
        RejectionReason = rejectionReason.Trim();
        Status = ServiceAdvancePaymentStatus.Rejected;
    }
}
