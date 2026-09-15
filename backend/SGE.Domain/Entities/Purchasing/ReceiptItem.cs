using SGE.Domain.Common;

namespace SGE.Domain.Entities.Purchasing;

public class ReceiptItem : BaseEntity
{
    public Guid ReceiptId { get; private set; }

    public Guid PurchaseOrderItemId { get; private set; }

    public decimal QuantityReceived { get; private set; }

    public string? Observation { get; private set; }

    public bool HasDivergence { get; private set; }

    public decimal DivergenceQuantity { get; private set; }

    public Receipt Receipt { get; private set; } = null!;

    public PurchaseOrderItem PurchaseOrderItem { get; private set; } = null!;

    private ReceiptItem()
    {
    }

    public ReceiptItem(
        Guid receiptId,
        Guid purchaseOrderItemId,
        decimal quantityReceived,
        string? observation,
        bool hasDivergence,
        decimal divergenceQuantity)
    {
        if (quantityReceived <= 0)
            throw new ArgumentException(
                "A quantidade recebida deve ser maior que zero.");

        if (hasDivergence && string.IsNullOrWhiteSpace(observation))
            throw new InvalidOperationException(
                "Recebimentos acima da quantidade pedida exigem justificativa.");

        ReceiptId = receiptId;
        PurchaseOrderItemId = purchaseOrderItemId;
        QuantityReceived = quantityReceived;
        Observation = observation;
        HasDivergence = hasDivergence;
        DivergenceQuantity = divergenceQuantity;
    }
}
