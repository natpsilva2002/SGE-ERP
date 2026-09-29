using SGE.Domain.Common;
using SGE.Domain.Entities.Administration;
using SGE.Domain.Enums;

namespace SGE.Domain.Entities.Purchasing;

public class ServiceOrderAmendment : BaseAuditableEntity
{
    public Guid ServiceOrderId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public decimal? ValueAdjustment { get; private set; }
    public decimal? QuantityAdjustment { get; private set; }
    public string? Observation { get; private set; }
    public ServiceOrderAmendmentStatus Status { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public decimal? ValueBeforeApproval { get; private set; }
    public decimal? ValueAfterApproval { get; private set; }
    public decimal? QuantityBeforeApproval { get; private set; }
    public decimal? QuantityAfterApproval { get; private set; }

    public ServiceOrder ServiceOrder { get; private set; } = null!;
    public User CreatedByUser { get; private set; } = null!;
    public User? ApprovedByUser { get; private set; }
    public ICollection<ServiceOrderAmendmentAttachment> Attachments { get; private set; } = new List<ServiceOrderAmendmentAttachment>();
    public ICollection<Approval> Approvals { get; private set; } = new List<Approval>();

    private ServiceOrderAmendment() { }

    public ServiceOrderAmendment(Guid serviceOrderId, Guid createdByUserId, string reason,
        decimal? valueAdjustment, decimal? quantityAdjustment, string? observation)
    {
        Validate(reason, valueAdjustment, quantityAdjustment);
        ServiceOrderId = serviceOrderId;
        CreatedByUserId = createdByUserId;
        Reason = reason.Trim();
        ValueAdjustment = valueAdjustment;
        QuantityAdjustment = quantityAdjustment;
        Observation = observation?.Trim();
        Status = ServiceOrderAmendmentStatus.Draft;
    }

    public void Update(string reason, decimal? valueAdjustment, decimal? quantityAdjustment, string? observation)
    {
        if (Status is not (ServiceOrderAmendmentStatus.Draft or ServiceOrderAmendmentStatus.Rejected))
            throw new InvalidOperationException("Somente adendos em rascunho ou rejeitados podem ser alterados.");
        Validate(reason, valueAdjustment, quantityAdjustment);
        Reason = reason.Trim();
        ValueAdjustment = valueAdjustment;
        QuantityAdjustment = quantityAdjustment;
        Observation = observation?.Trim();
        Status = ServiceOrderAmendmentStatus.Draft;
    }

    public void Submit()
    {
        if (Status is not (ServiceOrderAmendmentStatus.Draft or ServiceOrderAmendmentStatus.Rejected))
            throw new InvalidOperationException("O adendo não pode ser enviado neste estado.");
        Status = ServiceOrderAmendmentStatus.WaitingApproval;
    }

    public void Approve(Guid userId, ServiceOrder order)
    {
        if (Status != ServiceOrderAmendmentStatus.WaitingApproval)
            throw new InvalidOperationException("Somente adendos aguardando aprovação podem ser aprovados.");
        var valueBefore = order.CurrentContractedValue;
        var quantityBefore = order.CurrentContractedQuantity;
        var valueAfter = valueBefore + (ValueAdjustment ?? 0m);
        var quantityAfter = quantityBefore.HasValue
            ? quantityBefore.Value + (QuantityAdjustment ?? 0m)
            : QuantityAdjustment;
        if (valueAfter <= 0 || valueAfter < order.AmountPaid)
            throw new InvalidOperationException("O novo valor contratual não pode ser menor ou igual a zero nem inferior ao total já pago.");
        if (quantityAfter.HasValue && quantityAfter <= 0)
            throw new InvalidOperationException("A nova quantidade contratual deve ser maior que zero.");
        var committedQuantity = order.Measurements
            .Where(x => x.Status != ServiceMeasurementStatus.Rejected)
            .Sum(x => x.QuantityMeasured);
        if (quantityAfter.HasValue && quantityAfter.Value < committedQuantity)
            throw new InvalidOperationException("A nova quantidade contratual não pode ser menor que as medições já registradas.");
        var committedAmount = order.Measurements
            .Where(x => x.Status != ServiceMeasurementStatus.Rejected)
            .Sum(x => x.Amount);
        if (valueAfter < committedAmount)
            throw new InvalidOperationException("O novo valor contratual não pode ser menor que as medições já registradas.");

        ValueBeforeApproval = valueBefore;
        ValueAfterApproval = valueAfter;
        QuantityBeforeApproval = quantityBefore;
        QuantityAfterApproval = quantityAfter;
        ApprovedByUserId = userId;
        ApprovedAt = DateTime.UtcNow;
        Status = ServiceOrderAmendmentStatus.Approved;
    }

    public void Reject()
    {
        if (Status != ServiceOrderAmendmentStatus.WaitingApproval)
            throw new InvalidOperationException("Somente adendos aguardando aprovação podem ser rejeitados.");
        Status = ServiceOrderAmendmentStatus.Rejected;
    }

    private static void Validate(string reason, decimal? valueAdjustment, decimal? quantityAdjustment)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Informe o motivo do adendo.");
        if (valueAdjustment == 0 || quantityAdjustment == 0)
            throw new ArgumentException("Informe um ajuste diferente de zero ou deixe o campo vazio.");
        if (!valueAdjustment.HasValue && !quantityAdjustment.HasValue)
            throw new ArgumentException("Informe ao menos uma alteração de valor ou quantidade.");
    }
}
