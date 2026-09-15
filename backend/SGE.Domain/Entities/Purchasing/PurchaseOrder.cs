using SGE.Domain.Common;
using SGE.Domain.Entities.Administration;
using SGE.Domain.Entities.Companies;
using SGE.Domain.Enums;
namespace SGE.Domain.Entities.Purchasing;


public class PurchaseOrder : BaseSoftDeleteEntity
{
    public Guid QuotationId { get; private set; }

    public Guid SupplierId { get; private set; }

    public string Number { get; private set; } = string.Empty;

    public DateTime IssueDate { get; private set; }

    public DateTime? ExpectedDeliveryDate { get; private set; }

    public PurchaseOrderStatus Status { get; private set; }

    public decimal TotalValue { get; private set; }

    public decimal AmountPaid { get; private set; }

    public decimal AmountPending =>
        Math.Max(TotalValue - AmountPaid, 0);

    public PurchaseOrderPaymentStatus PaymentStatus { get; private set; }

    public string? PartialCloseReason { get; private set; }

    public int? DeliveryDays { get; private set; }

    public string? PaymentCondition { get; private set; }

    public int? InstallmentCount { get; private set; }

    public DateTime? ApprovedAt { get; private set; }

    public Guid? ApprovedByUserId { get; private set; }

    public DateTime? FirstApprovedAt { get; private set; }

    public Guid? FirstApprovedByUserId { get; private set; }

    public DateTime? SecondApprovedAt { get; private set; }

    public Guid? SecondApprovedByUserId { get; private set; }

    public DateTime? SentAt { get; private set; }

    public Guid? SentByUserId { get; private set; }

    public DateTime? PaymentApprovedAt { get; private set; }

    public Guid? PaymentApprovedByUserId { get; private set; }

    public Quotation Quotation { get; private set; } = null!;

    public Supplier Supplier { get; private set; } = null!;

    public User? FirstApprovedByUser { get; private set; }

    public User? SecondApprovedByUser { get; private set; }

    public User? PaymentApprovedByUser { get; private set; }

    public ICollection<PurchaseOrderItem> Items { get; private set; } = new List<PurchaseOrderItem>();

    public ICollection<Receipt> Receipts { get; private set; } = new List<Receipt>();

    public ICollection<Payment> Payments { get; private set; } = new List<Payment>();

    private PurchaseOrder()
    {
    }

    public PurchaseOrder(
        Guid quotationId,
        Guid supplierId,
        string number,
        DateTime? expectedDeliveryDate)
    {
        QuotationId = quotationId;
        SupplierId = supplierId;
        Number = number;
        ExpectedDeliveryDate = expectedDeliveryDate;
        IssueDate = DateTime.UtcNow;
        Status = PurchaseOrderStatus.Open;
        AmountPaid = 0;
        PaymentStatus = PurchaseOrderPaymentStatus.Unpaid;
    }

    public void AddItem(
        Guid itemId,
        decimal quantityOrdered,
        string unit,
        decimal unitPrice,
        decimal? negotiatedTotalValue = null,
        string? observation = null)
    {
        var item = new PurchaseOrderItem(
            Id,
            itemId,
            quantityOrdered,
            unit,
            unitPrice,
            negotiatedTotalValue,
            observation);

        Items.Add(item);
        RecalculateTotal();
    }

    public void UpdateTotal(decimal total)
    {
        TotalValue = total;
    }

    public void RecalculateTotal()
    {
        TotalValue = Items.Sum(x => x.TotalValue);
    }

    public void ChangeStatus(PurchaseOrderStatus status)
    {
        Status = status;
    }

    public void SetCommercialTerms(
        int? deliveryDays,
        string? paymentCondition,
        int? installmentCount)
    {
        if (deliveryDays < 0)
            throw new ArgumentException(
                "O prazo de entrega nao pode ser negativo.");

        if (installmentCount is <= 0)
            throw new ArgumentException(
                "A quantidade de parcelas deve ser maior que zero.");

        DeliveryDays = deliveryDays;
        PaymentCondition = string.IsNullOrWhiteSpace(paymentCondition)
            ? null
            : paymentCondition.Trim();
        InstallmentCount = installmentCount;
    }

    public void Approve(Guid userId)
    {
        if (Status == PurchaseOrderStatus.Open)
        {
            Status = PurchaseOrderStatus.WaitingSecondApproval;
            FirstApprovedAt = DateTime.UtcNow;
            FirstApprovedByUserId = userId;
            return;
        }

        if (Status == PurchaseOrderStatus.WaitingSecondApproval)
        {
            if (FirstApprovedByUserId == userId)
                throw new InvalidOperationException(
                    "O segundo aprovador deve ser diferente do primeiro aprovador.");

            Status = PurchaseOrderStatus.Approved;
            SecondApprovedAt = DateTime.UtcNow;
            SecondApprovedByUserId = userId;
            ApprovedAt = SecondApprovedAt;
            ApprovedByUserId = SecondApprovedByUserId;
            return;
        }

        throw new InvalidOperationException(
            "Apenas ordens de compra abertas ou aguardando segunda aprovacao podem ser aprovadas.");
    }

    public void MarkAsSent(Guid userId)
    {
        if (Status != PurchaseOrderStatus.Approved)
            throw new InvalidOperationException(
                "Apenas ordens de compra aprovadas podem ser marcadas como enviadas.");

        Status = PurchaseOrderStatus.Sent;
        SentAt = DateTime.UtcNow;
        SentByUserId = userId;
    }

    public void ApprovePayment(Guid userId)
    {
        if (!CanApprovePaymentByStatus())
            throw new InvalidOperationException(BuildPaymentApprovalStatusMessage());

        if (PaymentStatus == PurchaseOrderPaymentStatus.Paid ||
            AmountPending <= 0)
            throw new InvalidOperationException(
                "A ordem de compra ja esta totalmente paga.");

        if (PaymentApprovedAt.HasValue)
            throw new InvalidOperationException(
                "O pagamento da ordem de compra ja foi autorizado.");

        PaymentApprovedAt = DateTime.UtcNow;
        PaymentApprovedByUserId = userId;
    }

    public void EnsureCanGenerateOfficialPdf()
    {
        if (Status == PurchaseOrderStatus.Open)
            throw new InvalidOperationException(
                "O PDF oficial da ordem de compra so pode ser gerado apos aprovacao.");

        if (Status == PurchaseOrderStatus.WaitingSecondApproval)
            throw new InvalidOperationException(
                "A ordem de compra ainda aguarda a segunda aprovação.");

        if (Status == PurchaseOrderStatus.Cancelled)
            throw new InvalidOperationException(
                "Nao e possivel gerar PDF oficial de uma ordem de compra cancelada.");
    }

    public void EnsureCanReceive()
    {
        if (Status == PurchaseOrderStatus.Open ||
            Status == PurchaseOrderStatus.WaitingSecondApproval)
            throw new InvalidOperationException(
                "A ordem de compra precisa estar aprovada para receber materiais.");

        if (Status == PurchaseOrderStatus.Cancelled ||
            Status == PurchaseOrderStatus.Received ||
            Status == PurchaseOrderStatus.PartiallyCompleted ||
            Status == PurchaseOrderStatus.Completed)
            throw new InvalidOperationException(
                "Nao e possivel receber uma ordem de compra cancelada, ja recebida, encerrada parcialmente ou concluida.");
    }

    public void RefreshReceiptStatus()
    {
        if (Items.All(x => x.QuantityPending == 0))
        {
            Status = PurchaseOrderStatus.Received;
            return;
        }

        if (Items.Any(x => x.QuantityReceived > 0))
            Status = PurchaseOrderStatus.PartiallyReceived;
    }

    public void Cancel()
    {
        if (Items.Any(x => x.QuantityReceived > 0))
            throw new InvalidOperationException(
                "Nao e possivel cancelar uma ordem de compra que ja possui recebimento.");

        Status = PurchaseOrderStatus.Cancelled;
    }

    public void ClosePartially(string reason)
    {
        if (Status != PurchaseOrderStatus.PartiallyReceived)
            throw new InvalidOperationException(
                "Apenas ordens parcialmente recebidas podem ser encerradas parcialmente.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException(
                "O motivo do encerramento parcial e obrigatorio.");

        PartialCloseReason = reason;
        Status = PurchaseOrderStatus.PartiallyCompleted;
    }

    public void EnsureCanPay()
    {
        if (Status == PurchaseOrderStatus.Cancelled ||
            Status == PurchaseOrderStatus.Completed)
            throw new InvalidOperationException(
                "Nao e possivel pagar uma ordem de compra cancelada ou concluida.");

        if (!CanApprovePaymentByStatus())
            throw new InvalidOperationException(BuildPaymentApprovalStatusMessage());

        if (!PaymentApprovedAt.HasValue)
            throw new InvalidOperationException(
                "O pagamento da ordem de compra ainda não foi autorizado.");

        if (PaymentStatus == PurchaseOrderPaymentStatus.Paid)
            throw new InvalidOperationException(
                "A ordem de compra ja esta totalmente paga.");
    }

    public void RegisterPayment(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException(
                "O valor do pagamento deve ser maior que zero.");

        EnsureCanPay();

        if (amount > AmountPending)
            throw new InvalidOperationException(
                "O valor do pagamento nao pode ser maior que o valor pendente da ordem de compra.");

        AmountPaid += amount;
        RefreshPaymentStatus();
    }

    public void RefreshPaymentStatus()
    {
        if (AmountPaid <= 0)
        {
            PaymentStatus = PurchaseOrderPaymentStatus.Unpaid;
            return;
        }

        if (AmountPending == 0)
        {
            PaymentStatus = PurchaseOrderPaymentStatus.Paid;
            return;
        }

        PaymentStatus = PurchaseOrderPaymentStatus.PartiallyPaid;
    }

    private bool CanApprovePaymentByStatus()
    {
        return Status == PurchaseOrderStatus.Approved ||
            Status == PurchaseOrderStatus.Sent ||
            Status == PurchaseOrderStatus.PartiallyReceived ||
            Status == PurchaseOrderStatus.Received ||
            Status == PurchaseOrderStatus.PartiallyCompleted;
    }

    private string BuildPaymentApprovalStatusMessage()
    {
        if (Status == PurchaseOrderStatus.WaitingSecondApproval)
            return "A ordem de compra ainda aguarda a segunda aprovação.";

        if (Status == PurchaseOrderStatus.Open)
            return "A ordem de compra ainda nao foi aprovada.";

        if (Status == PurchaseOrderStatus.Cancelled)
            return "Nao e possivel autorizar pagamento de uma ordem de compra cancelada.";

        return "A ordem de compra nao esta em status compativel para autorizacao de pagamento.";
    }
    public void Update(
    string number,
    DateTime? expectedDeliveryDate)
{
    Number = number;
    ExpectedDeliveryDate = expectedDeliveryDate;
}
}
