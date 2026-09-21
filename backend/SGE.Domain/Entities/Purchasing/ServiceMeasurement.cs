using SGE.Domain.Common;
using SGE.Domain.Entities.Administration;
using SGE.Domain.Enums;

namespace SGE.Domain.Entities.Purchasing;

public class ServiceMeasurement : BaseEntity
{
    public Guid ServiceOrderId { get; private set; }

    public string? MeasurementNumber { get; private set; }

    public DateTime MeasurementDate { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal QuantityMeasured { get; private set; }

    public string Unit { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public string? Observation { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public ServiceMeasurementStatus Status { get; private set; }

    public DateTime? ApprovedAt { get; private set; }

    public Guid? ApprovedByUserId { get; private set; }

    public DateTime? RejectedAt { get; private set; }

    public Guid? RejectedByUserId { get; private set; }

    public string? RejectionReason { get; private set; }

    public ServiceOrder ServiceOrder { get; private set; } = null!;

    public User CreatedByUser { get; private set; } = null!;

    public User? ApprovedByUser { get; private set; }

    public User? RejectedByUser { get; private set; }

    public ICollection<ServiceMeasurementAttachment> Attachments { get; private set; } =
        new List<ServiceMeasurementAttachment>();

    private ServiceMeasurement()
    {
    }

    public ServiceMeasurement(
        Guid serviceOrderId,
        string measurementNumber,
        DateTime measurementDate,
        string description,
        decimal quantityMeasured,
        string? unit,
        decimal amount,
        Guid createdByUserId,
        string? observation)
    {
        if (string.IsNullOrWhiteSpace(measurementNumber))
            throw new ArgumentException("O numero da medicao e obrigatorio.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("A descricao da medicao e obrigatoria.");

        if (amount <= 0)
            throw new ArgumentException("O valor medido deve ser maior que zero.");

        if (quantityMeasured <= 0)
            throw new ArgumentException("A quantidade medida deve ser maior que zero.");

        ServiceOrderId = serviceOrderId;
        MeasurementNumber = measurementNumber.Trim();
        MeasurementDate = NormalizeDate(measurementDate);
        Description = description.Trim();
        QuantityMeasured = quantityMeasured;
        Unit = string.IsNullOrWhiteSpace(unit) ? "VB" : unit.Trim();
        Amount = amount;
        CreatedByUserId = createdByUserId;
        Observation = string.IsNullOrWhiteSpace(observation) ? null : observation.Trim();
        CreatedAt = DateTime.UtcNow;
        Status = ServiceMeasurementStatus.Approved;
    }

    public void Update(
        DateTime measurementDate,
        string description,
        decimal quantityMeasured,
        string? unit,
        decimal amount,
        string? observation)
    {
        EnsureDraft("Apenas medicoes em rascunho podem ser alteradas.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("A descricao da medicao e obrigatoria.");

        if (amount <= 0)
            throw new ArgumentException("O valor medido deve ser maior que zero.");

        if (quantityMeasured <= 0)
            throw new ArgumentException("A quantidade medida deve ser maior que zero.");

        MeasurementDate = NormalizeDate(measurementDate);
        Description = description.Trim();
        QuantityMeasured = quantityMeasured;
        Unit = string.IsNullOrWhiteSpace(unit) ? Unit : unit.Trim();
        Amount = amount;
        Observation = string.IsNullOrWhiteSpace(observation) ? null : observation.Trim();
    }

    public void Submit()
    {
        EnsureDraft("Apenas medicoes em rascunho podem ser enviadas para aprovacao.");

        Status = ServiceMeasurementStatus.WaitingApproval;
    }

    public void Approve(Guid approvedByUserId)
    {
        if (Status != ServiceMeasurementStatus.WaitingApproval)
            throw new InvalidOperationException(
                "Apenas medicoes aguardando aprovacao podem ser aprovadas.");

        Status = ServiceMeasurementStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAt = DateTime.UtcNow;
    }

    public void Reject(Guid rejectedByUserId, string rejectionReason)
    {
        if (Status != ServiceMeasurementStatus.WaitingApproval)
            throw new InvalidOperationException(
                "Apenas medicoes aguardando aprovacao podem ser rejeitadas.");

        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new ArgumentException("O motivo da rejeicao e obrigatorio.");

        Status = ServiceMeasurementStatus.Rejected;
        RejectedByUserId = rejectedByUserId;
        RejectedAt = DateTime.UtcNow;
        RejectionReason = rejectionReason.Trim();
    }

    private void EnsureDraft(string message)
    {
        if (Status != ServiceMeasurementStatus.Draft)
            throw new InvalidOperationException(message);
    }

    private static DateTime NormalizeDate(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
