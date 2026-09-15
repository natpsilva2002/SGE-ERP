using SGE.Domain.Common;
using SGE.Domain.Entities.Administration;
using SGE.Domain.Enums;

namespace SGE.Domain.Entities.Purchasing;

public class Payment : BaseEntity
{
    public Guid PurchaseOrderId { get; private set; }

    public Guid PaidByUserId { get; private set; }

    public DateTime PaymentDate { get; private set; }

    public decimal Amount { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    public string? TransactionReference { get; private set; }

    public DateTime? DueDate { get; private set; }

    public string? InvoiceNumber { get; private set; }

    public string? InvoiceFileName { get; private set; }

    public string? InvoiceFilePath { get; private set; }

    public string? Observation { get; private set; }

    public PaymentStatus Status { get; private set; }

    public PurchaseOrder PurchaseOrder { get; private set; } = null!;

    public User PaidByUser { get; private set; } = null!;

    private Payment()
    {
    }

    public Payment(
        Guid purchaseOrderId,
        Guid paidByUserId,
        decimal amount,
        DateTime paymentDate,
        PaymentMethod paymentMethod,
        string? transactionReference,
        DateTime? dueDate,
        string? invoiceNumber,
        string? invoiceFileName,
        string? invoiceFilePath,
        string? observation)
    {
        if (amount <= 0)
            throw new ArgumentException(
                "O valor do pagamento deve ser maior que zero.");

        if (!Enum.IsDefined(paymentMethod))
            throw new ArgumentException(
                "O metodo de pagamento informado e invalido.");

        PurchaseOrderId = purchaseOrderId;
        PaidByUserId = paidByUserId;
        Amount = amount;
        PaymentMethod = paymentMethod;
        TransactionReference = transactionReference;
        DueDate = dueDate;
        InvoiceNumber = invoiceNumber;
        InvoiceFileName = invoiceFileName;
        InvoiceFilePath = invoiceFilePath;
        Observation = observation;
        PaymentDate = paymentDate;
        Status = PaymentStatus.Registered;
    }
}
