namespace SGE.Application.DTOs.PurchaseOrder;

public class PurchaseOrderReceiptViewItemDto
{
    public Guid PurchaseOrderItemId { get; set; }

    public Guid ItemId { get; set; }

    public string ItemDescription { get; set; } = string.Empty;

    public decimal QuantityOrdered { get; set; }

    public decimal QuantityReceived { get; set; }

    public decimal QuantityPending { get; set; }

    public string Unit { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }
}
