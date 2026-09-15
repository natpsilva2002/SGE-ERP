using SGE.Domain.Common;
using SGE.Domain.Entities.Catalog;

namespace SGE.Domain.Entities.Purchasing;


public class PurchaseOrderItem : BaseEntity
{
    public Guid PurchaseOrderId { get; private set; }

    public Guid ItemId { get; private set; }

    public decimal QuantityOrdered { get; private set; }

    public decimal QuantityReceived { get; private set; }

    public decimal QuantityPending =>
        Math.Max(QuantityOrdered - QuantityReceived, 0);

    public string Unit { get; private set; } = string.Empty;

    public decimal UnitPrice { get; private set; }

    public decimal? NegotiatedTotalValue { get; private set; }

    public string? Observation { get; private set; }

    public decimal TotalValue => NegotiatedTotalValue ?? QuantityOrdered * UnitPrice;

    public PurchaseOrder PurchaseOrder { get; private set; } = null!;

    public Item Item { get; private set; } = null!;

    private PurchaseOrderItem()
    {
    }

    public PurchaseOrderItem(
        Guid purchaseOrderId,
        Guid itemId,
        decimal quantityOrdered,
        string unit,
        decimal unitPrice,
        decimal? negotiatedTotalValue = null,
        string? observation = null)
    {
        if (quantityOrdered <= 0)
            throw new ArgumentException(
                "A quantidade do pedido deve ser maior que zero.");

        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException(
                "A unidade e obrigatoria.");

        if (unitPrice <= 0)
            throw new ArgumentException(
                "O preco unitario deve ser maior que zero.");

        if (negotiatedTotalValue.HasValue && negotiatedTotalValue.Value <= 0)
            throw new ArgumentException(
                "O valor total negociado deve ser maior que zero.");

        PurchaseOrderId = purchaseOrderId;
        ItemId = itemId;
        QuantityOrdered = quantityOrdered;
        QuantityReceived = 0;
        Unit = unit;
        UnitPrice = unitPrice;
        NegotiatedTotalValue = negotiatedTotalValue;
        Observation = string.IsNullOrWhiteSpace(observation)
            ? null
            : observation.Trim();
    }

    public void Receive(decimal quantityReceived)
    {
        if (quantityReceived <= 0)
            throw new ArgumentException(
                "A quantidade recebida deve ser maior que zero.");

        QuantityReceived += quantityReceived;
    }

    public void Update(
        decimal quantityOrdered,
        string unit,
        decimal unitPrice)
    {
        if (quantityOrdered <= 0)
            throw new ArgumentException(
                "A quantidade do pedido deve ser maior que zero.");

        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException(
                "A unidade e obrigatoria.");

        if (unitPrice <= 0)
            throw new ArgumentException(
                "O preco unitario deve ser maior que zero.");

        QuantityOrdered = quantityOrdered;
        Unit = unit;
        UnitPrice = unitPrice;
    }
}
