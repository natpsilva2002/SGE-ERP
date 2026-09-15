using SGE.Domain.Common;
using SGE.Domain.Entities.Catalog;

namespace SGE.Domain.Entities.Purchasing;

public class PurchaseRequestItem : BaseEntity
{
    public Guid PurchaseRequestId { get; private set; }

    public Guid ItemId { get; private set; }

    public decimal Quantity { get; private set; }

    public string Unit { get; private set; } = string.Empty;

    public string? Observation { get; private set; }

    public PurchaseRequest PurchaseRequest { get; private set; } = null!;

    public Item Item { get; private set; } = null!;

    private PurchaseRequestItem()
    {
    }

    public PurchaseRequestItem(
        Guid purchaseRequestId,
        Guid itemId,
        decimal quantity,
        string unit,
        string? observation)
    {
        if (quantity <= 0)
            throw new ArgumentException(
                "A quantidade deve ser maior que zero.");

        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException(
                "A unidade é obrigatória.");

        PurchaseRequestId = purchaseRequestId;
        ItemId = itemId;
        Quantity = quantity;
        Unit = unit;
        Observation = observation;
    }

    public void Update(
        decimal quantity,
        string unit,
        string? observation)
    {
        if (quantity <= 0)
            throw new ArgumentException(
                "A quantidade deve ser maior que zero.");

        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException(
                "A unidade é obrigatória.");

        Quantity = quantity;
        Unit = unit;
        Observation = observation;
    }
}