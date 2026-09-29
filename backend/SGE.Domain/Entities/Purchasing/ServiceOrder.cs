using SGE.Domain.Common;
using SGE.Domain.Entities.Administration;
using SGE.Domain.Entities.Companies;
using SGE.Domain.Enums;

namespace SGE.Domain.Entities.Purchasing;

public class ServiceOrder : BaseSoftDeleteEntity
{
    public Guid PurchaseRequestId { get; private set; }

    public Guid WorkId { get; private set; }

    public Guid SupplierId { get; private set; }

    public string Number { get; private set; } = string.Empty;

    public string ServiceDescription { get; private set; } = string.Empty;

    public string? ServiceSpecification { get; private set; }

    public decimal? EstimatedQuantity { get; private set; }

    public string? Unit { get; private set; }

    public decimal ContractedValue { get; private set; }

    public decimal CurrentContractedValue => ContractedValue + Amendments
        .Where(x => x.Status == ServiceOrderAmendmentStatus.Approved)
        .Sum(x => x.ValueAdjustment ?? 0m);

    public decimal? CurrentContractedQuantity
    {
        get
        {
            var approvedAdjustments = Amendments
                .Where(x => x.Status == ServiceOrderAmendmentStatus.Approved)
                .Sum(x => x.QuantityAdjustment ?? 0m);
            return EstimatedQuantity.HasValue
                ? EstimatedQuantity.Value + approvedAdjustments
                : approvedAdjustments == 0 ? null : approvedAdjustments;
        }
    }

    public string? PaymentCondition { get; private set; }

    public int? InstallmentCount { get; private set; }

    public string? ContractFileName { get; private set; }

    public string? ContractFilePath { get; private set; }

    public DateTime? ContractUploadedAt { get; private set; }

    public Guid? ContractUploadedByUserId { get; private set; }

    public ServiceOrderExecutionStatus ExecutionStatus { get; private set; }

    public decimal AmountPaid { get; private set; }

    public decimal AmountPending =>
        Math.Max(CurrentContractedValue - AmountPaid, 0);

    public ServiceOrderPaymentStatus PaymentStatus { get; private set; }

    public PurchaseRequest PurchaseRequest { get; private set; } = null!;

    public Work Work { get; private set; } = null!;

    public Supplier Supplier { get; private set; } = null!;

    public User? ContractUploadedByUser { get; private set; }

    public ICollection<ServiceMeasurement> Measurements { get; private set; } = new List<ServiceMeasurement>();

    public ICollection<ServiceOrderPayment> Payments { get; private set; } = new List<ServiceOrderPayment>();

    public ICollection<ServiceAdvancePaymentRequest> AdvancePaymentRequests { get; private set; } =
        new List<ServiceAdvancePaymentRequest>();

    public ICollection<ServiceOrderAttachment> Attachments { get; private set; } =
        new List<ServiceOrderAttachment>();

    public ICollection<ServiceOrderAmendment> Amendments { get; private set; } =
        new List<ServiceOrderAmendment>();

    private ServiceOrder()
    {
    }

    public ServiceOrder(
        Guid purchaseRequestId,
        Guid workId,
        Guid supplierId,
        string number,
        string serviceDescription,
        string? serviceSpecification,
        decimal? estimatedQuantity,
        string? unit,
        decimal contractedValue,
        string? paymentCondition,
        int? installmentCount = null)
    {
        if (supplierId == Guid.Empty)
            throw new ArgumentException("O prestador de servico e obrigatorio.");

        if (contractedValue <= 0)
            throw new ArgumentException("O valor contratado deve ser maior que zero.");

        if (installmentCount.HasValue && installmentCount.Value <= 0)
            throw new ArgumentException("A quantidade de parcelas deve ser maior que zero.");
        if (!estimatedQuantity.HasValue || estimatedQuantity <= 0 || string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("A quantidade contratada e a unidade de medicao sao obrigatorias.");

        PurchaseRequestId = purchaseRequestId;
        WorkId = workId;
        SupplierId = supplierId;
        Number = number;
        ServiceDescription = serviceDescription;
        ServiceSpecification = serviceSpecification;
        EstimatedQuantity = estimatedQuantity;
        Unit = unit;
        ContractedValue = contractedValue;
        PaymentCondition = paymentCondition;
        InstallmentCount = installmentCount;
        ExecutionStatus = ServiceOrderExecutionStatus.WaitingContract;
        PaymentStatus = ServiceOrderPaymentStatus.Unpaid;
        AmountPaid = 0;
    }

    public void AttachContract(
        string originalFileName,
        string filePath,
        Guid uploadedByUserId)
    {
        EnsureContractTermsEditable();
        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("O nome do arquivo do contrato e obrigatorio.");

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("O caminho do arquivo do contrato e obrigatorio.");

        ContractFileName = originalFileName;
        ContractFilePath = filePath;
        ContractUploadedByUserId = uploadedByUserId;
        ContractUploadedAt = DateTime.UtcNow;
    }

    public void Release()
    {
        if (ExecutionStatus != ServiceOrderExecutionStatus.WaitingContract)
            throw new InvalidOperationException(
                "Apenas ordens de servico aguardando contrato podem ser liberadas.");

        if (string.IsNullOrWhiteSpace(ContractFilePath))
            throw new InvalidOperationException(
                "Nao e possivel liberar a ordem de servico sem contrato anexado.");

        ExecutionStatus = ServiceOrderExecutionStatus.Released;
    }

    public void UpdateContractTerms(
        Guid supplierId,
        decimal contractedValue,
        decimal quantity,
        string unit,
        string? paymentCondition,
        int? installmentCount)
    {
        EnsureContractTermsEditable();
        if (supplierId == Guid.Empty)
            throw new ArgumentException("O prestador de servico e obrigatorio.");
        if (contractedValue <= 0)
            throw new ArgumentException("O valor contratado deve ser maior que zero.");
        if (quantity <= 0 || string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("A quantidade contratada e a unidade de medicao sao obrigatorias.");
        if (installmentCount.HasValue && installmentCount.Value <= 0)
            throw new ArgumentException("A quantidade de parcelas deve ser maior que zero.");
        if (contractedValue < AmountPaid)
            throw new InvalidOperationException("O valor contratual não pode ficar abaixo do total já pago.");
        var committedAmount = Measurements.Where(x => x.Status != ServiceMeasurementStatus.Rejected).Sum(x => x.Amount);
        if (contractedValue < committedAmount)
            throw new InvalidOperationException("O valor contratual não pode ficar abaixo das medições já registradas.");
        var committedQuantity = Measurements.Where(x => x.Status != ServiceMeasurementStatus.Rejected).Sum(x => x.QuantityMeasured);
        if (quantity < committedQuantity)
            throw new InvalidOperationException("A quantidade contratada não pode ficar abaixo das medições já registradas.");

        SupplierId = supplierId;
        ContractedValue = contractedValue;
        EstimatedQuantity = quantity;
        Unit = unit.Trim();
        PaymentCondition = paymentCondition;
        InstallmentCount = installmentCount;
    }

    public void StartExecution()
    {
        if (ExecutionStatus == ServiceOrderExecutionStatus.Released)
        {
            ExecutionStatus = ServiceOrderExecutionStatus.InProgress;
            return;
        }

        if (ExecutionStatus != ServiceOrderExecutionStatus.InProgress)
            throw new InvalidOperationException(
                "Apenas ordens de servico liberadas ou em execucao podem receber medicoes.");
    }

    public void RefreshExecutionByApprovedAmount(decimal approvedMeasuredAmount)
    {
        if (approvedMeasuredAmount < 0)
            throw new ArgumentException("O valor medido aprovado nao pode ser negativo.");

        if (approvedMeasuredAmount > CurrentContractedValue)
            throw new InvalidOperationException(
                "O valor medido aprovado nao pode ultrapassar o valor contratado.");

        ExecutionStatus = approvedMeasuredAmount == CurrentContractedValue
            ? ServiceOrderExecutionStatus.Completed
            : ServiceOrderExecutionStatus.InProgress;
    }

    public void RegisterPayment(decimal amount)
    {
        EnsureReleasedForFinancialOperation();
        if (amount <= 0)
            throw new ArgumentException("O valor do pagamento deve ser maior que zero.");

        if (AmountPaid + amount > CurrentContractedValue)
            throw new InvalidOperationException(
                "O pagamento nao pode ultrapassar o valor contratado da ordem de servico.");

        AmountPaid += amount;
        RefreshPaymentStatus();
    }

    public void RefreshPaymentStatus()
    {
        if (AmountPaid <= 0)
        {
            PaymentStatus = ServiceOrderPaymentStatus.Unpaid;
            return;
        }

        PaymentStatus = AmountPaid == CurrentContractedValue
            ? ServiceOrderPaymentStatus.Paid
            : ServiceOrderPaymentStatus.PartiallyPaid;
    }

    public void EnsureReleasedForFinancialOperation()
    {
        if (ExecutionStatus == ServiceOrderExecutionStatus.WaitingContract)
            throw new InvalidOperationException(
                "A Ordem de Serviço precisa ser liberada para execução antes de registrar pagamentos.");
    }

    private void EnsureContractTermsEditable()
    {
        if (ExecutionStatus != ServiceOrderExecutionStatus.WaitingContract)
            throw new InvalidOperationException(
                "Os dados contratuais da Ordem de Serviço não podem ser alterados após a liberação para execução.");
    }
}
