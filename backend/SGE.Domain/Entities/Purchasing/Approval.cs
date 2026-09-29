using SGE.Domain.Common;
using SGE.Domain.Entities.Administration;
using SGE.Domain.Enums;

namespace SGE.Domain.Entities.Purchasing;

public class Approval : BaseSoftDeleteEntity
{
    public ApprovalType Type { get; private set; }

    public ApprovalStatus Status { get; private set; }

    public Guid? PurchaseRequestId { get; private set; }

    public Guid? QuotationId { get; private set; }

    public Guid? ServiceOrderAmendmentId { get; private set; }

    public Guid UserId { get; private set; }

    public string? Observation { get; private set; }

    public DateTime? ApprovalDate { get; private set; }

    public PurchaseRequest? PurchaseRequest { get; private set; }

    public Quotation? Quotation { get; private set; }

    public ServiceOrderAmendment? ServiceOrderAmendment { get; private set; }

    public User User { get; private set; } = null!;

    private Approval()
    {
    }

    public Approval(
        ApprovalType type,
        Guid? purchaseRequestId,
        Guid? quotationId,
        Guid userId,
        ApprovalStatus status,
        string? observation,
        Guid? serviceOrderAmendmentId = null)
    {
        ValidateTarget(type, purchaseRequestId, quotationId, serviceOrderAmendmentId);

        Type = type;
        Status = status;
        PurchaseRequestId = purchaseRequestId;
        QuotationId = quotationId;
        ServiceOrderAmendmentId = serviceOrderAmendmentId;
        UserId = userId;
        Observation = observation;

        if (status != ApprovalStatus.Pending)
            ApprovalDate = DateTime.UtcNow;
    }

    public void Approve(Guid userId, string? observation)
    {
        Decide(userId, ApprovalStatus.Approved, observation);
    }

    public void Reject(Guid userId, string? observation)
    {
        Decide(userId, ApprovalStatus.Rejected, observation);
    }

    private void Decide(
        Guid userId,
        ApprovalStatus status,
        string? observation)
    {
        if (Status != ApprovalStatus.Pending)
            throw new InvalidOperationException(
                "A aprovacao ja foi decidida.");

        UserId = userId;
        Status = status;
        Observation = observation;
        ApprovalDate = DateTime.UtcNow;
    }

    private static void ValidateTarget(
        ApprovalType type,
        Guid? purchaseRequestId,
        Guid? quotationId,
        Guid? serviceOrderAmendmentId)
    {
        if (type == ApprovalType.PurchaseRequest &&
            (!purchaseRequestId.HasValue || quotationId.HasValue || serviceOrderAmendmentId.HasValue))
            throw new ArgumentException(
                "A aprovacao de solicitacao deve possuir apenas PurchaseRequestId.");

        if (type == ApprovalType.Quotation &&
            (!quotationId.HasValue || purchaseRequestId.HasValue || serviceOrderAmendmentId.HasValue))
            throw new ArgumentException(
                "A aprovacao de cotacao deve possuir apenas QuotationId.");

        if (type == ApprovalType.ServiceOrderAmendment &&
            (!serviceOrderAmendmentId.HasValue || purchaseRequestId.HasValue || quotationId.HasValue))
            throw new ArgumentException("A aprovação do adendo deve possuir apenas ServiceOrderAmendmentId.");

        if (type is not (ApprovalType.PurchaseRequest or ApprovalType.Quotation or ApprovalType.ServiceOrderAmendment))
            throw new ArgumentException(
                "Tipo de aprovacao invalido.");
    }
}
