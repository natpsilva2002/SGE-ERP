using SGE.Domain.Common;
using SGE.Domain.Entities.Administration;
using SGE.Domain.Enums;

namespace SGE.Domain.Entities.Purchasing;

public class Receipt : BaseEntity
{
    public Guid PurchaseOrderId { get; private set; }

    public Guid ReceivedByUserId { get; private set; }

    public DateTime ReceiptDate { get; private set; }

    public string? Observation { get; private set; }

    public string? InvoiceNumber { get; private set; }

    public string? InvoiceFileName { get; private set; }

    public string? InvoiceFilePath { get; private set; }

    public ReceiptStatus Status { get; private set; }

    public PurchaseOrder PurchaseOrder { get; private set; } = null!;

    public User ReceivedByUser { get; private set; } = null!;

    public ICollection<ReceiptItem> Items { get; private set; } = new List<ReceiptItem>();

    private Receipt()
    {
    }

    public Receipt(
        Guid purchaseOrderId,
        Guid receivedByUserId,
        string? observation)
    {
        PurchaseOrderId = purchaseOrderId;
        ReceivedByUserId = receivedByUserId;
        Observation = observation;
        ReceiptDate = DateTime.UtcNow;
        Status = ReceiptStatus.Registered;
    }

    public void AttachInvoice(
        string? invoiceNumber,
        string? invoiceFileName,
        string? invoiceFilePath)
    {
        InvoiceNumber = string.IsNullOrWhiteSpace(invoiceNumber)
            ? null
            : invoiceNumber.Trim();
        InvoiceFileName = string.IsNullOrWhiteSpace(invoiceFileName)
            ? null
            : invoiceFileName.Trim();
        InvoiceFilePath = string.IsNullOrWhiteSpace(invoiceFilePath)
            ? null
            : invoiceFilePath.Trim();
    }

    public void AddItem(
        Guid purchaseOrderItemId,
        decimal quantityReceived,
        string? observation,
        bool hasDivergence,
        decimal divergenceQuantity)
    {
        Items.Add(new ReceiptItem(
            Id,
            purchaseOrderItemId,
            quantityReceived,
            observation,
            hasDivergence,
            divergenceQuantity));
    }
}
